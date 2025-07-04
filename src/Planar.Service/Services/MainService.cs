﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.SystemJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Services;

public class MainService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MainService> _logger;
    private readonly SchedulerUtil _schedulerUtil;
    private readonly IServiceProvider _serviceProvider;

    public MainService(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
    {
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _logger = _serviceProvider.GetRequiredService<ILogger<MainService>>();
        _schedulerUtil = _serviceProvider.GetRequiredService<SchedulerUtil>();
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        _logger.LogInformation("service environment: {Environment}", AppSettings.General.Environment);

        await LoadGlobalConfigInner(stoppingToken);

        await LoadMonitorHooks();

        await ScheduleSystemJobs(stoppingToken);

        await JoinToCluster(stoppingToken);
    }

    public void Shutdown()
    {
        var removeTask = RemoveSchedulerCluster();
        if (_serviceProvider.GetService(typeof(IHostApplicationLifetime)) is IHostApplicationLifetime app)
        {
            try
            {
                removeTask.Wait(3000);
            }
            catch
            {
                // *** IGNORE EXCEPTION *** //
            }
            app.StopApplication();
        }
        Global.Clear();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
        {
            return;
        }

        _lifetime.ApplicationStopping.Register(async () =>
        {
            _logger.LogInformation("IsCancellationRequested = {Value}", stoppingToken.IsCancellationRequested);
            try
            {
                await RemoveSchedulerCluster();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "fail to {Operation}", nameof(RemoveSchedulerCluster));
            }

            try
            {
                await _schedulerUtil.Shutdown(stoppingToken);
            }
            catch
            {
                // *** ignore exceptions *** //
            }
        });

        _ = Run(stoppingToken)
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogError(t.Exception, "unhandled exception: {Message}", t.Exception.Message);
                }
            }, stoppingToken);

        ////var waiter = new CancellationTokenAwaiter(stoppingToken);

        ////waiter.OnCompleted(() =>
        ////{
        ////    _logger.LogInformation("IsCancellationRequested = {Value}", stoppingToken.IsCancellationRequested);
        ////    try
        ////    {
        ////        RemoveSchedulerCluster().Wait();
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        _logger.LogWarning(ex, "fail to {Operation}", nameof(RemoveSchedulerCluster));
        ////    }

        ////    try
        ////    {
        ////        _schedulerUtil.Shutdown(stoppingToken).Wait();
        ////    }
        ////    catch
        ////    {
        ////        // *** ignore exceptions *** //
        ////    }
        ////});

        await Task.CompletedTask;
    }

    private void SafeSystemScan(MonitorEvents @event, MonitorSystemInfo info, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        MonitorUtil.SafeSystemScan(_serviceProvider, _logger, @event, info, exception, cancellationToken);
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        using var reg1 = lifetime.ApplicationStarted.Register(startedSource.SetResult);
        using var reg2 = stoppingToken.Register(cancelledSource.SetResult);

        Task completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task).ConfigureAwait(false);

        // If the completed tasks was the "app started" task, return true, otherwise false
        return completedTask == startedSource.Task;
    }

    #region Initialize Scheduler

    public async Task LoadGlobalConfig(CancellationToken stoppingToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ConfigDomain>();
        await config.Flush(stoppingToken);
    }

    public async Task ScheduleSystemJobs(CancellationToken stoppingToken = default)
    {
        try
        {
            _logger.LogInformation("Initialize: {Operation}", nameof(ScheduleSystemJobs));
            await PersistDataJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await ClusterHealthCheckJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await ClearHistoryJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await StatisticsJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await MonitorJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await CircuitBreakerJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            await SchedulerHealthCheckJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);

            await SummaryReportJob.Schedule(_schedulerUtil.Scheduler, SummaryReportJob.ReportName, stoppingToken);
            await PausedReportJob.Schedule(_schedulerUtil.Scheduler, PausedReportJob.ReportName, stoppingToken);
            await AlertsReportJob.Schedule(_schedulerUtil.Scheduler, AlertsReportJob.ReportName, stoppingToken);
            await AuditsReportJob.Schedule(_schedulerUtil.Scheduler, AuditsReportJob.ReportName, stoppingToken);
            await TraceReportJob.Schedule(_schedulerUtil.Scheduler, TraceReportJob.ReportName, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(ScheduleSystemJobs));
            Shutdown();
        }
    }

    private async Task JoinToCluster(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var util = scope.ServiceProvider.GetRequiredService<ClusterUtil>();

        // === Single Node ===
        if (!AppSettings.Cluster.Clustering)
        {
            try
            {
                _logger.LogInformation("Initialize: {Operation}", "Register Current Node");
                await util.Join();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to {Operation}", "Register Current Node");
                await _schedulerUtil.Stop(stoppingToken);
                await _schedulerUtil.Shutdown(stoppingToken);
                Shutdown();
            }
        }

        // === Cluster (Multi-Nodes) ===
        try
        {
            _logger.LogInformation("Initialize: {Operation}", nameof(JoinToCluster));

            var nodes = await util.GetAllNodes();
            ClusterUtil.ValidateClusterConflict(nodes);

            var liveNodes = nodes.Where(n => n.LiveNode && !n.IsCurrentNode).ToList();
            var deadNodes = nodes.Where(n => !n.LiveNode && !n.IsCurrentNode).ToList();
            LogDeadNodes(deadNodes);

            if (await util.HealthCheck(liveNodes))
            {
                await util.Join();
                LogClustering();

                // Monitoring
                var info = new MonitorSystemInfo("Cluster node join to {{MachineName}}");
                info.MessagesParameters.Add("Port", AppSettings.General.ApiPort.ToString());
                info.MessagesParameters.Add("InstanceId", _schedulerUtil.SchedulerInstanceId);
                info.AddMachineName();
                SafeSystemScan(MonitorEvents.ClusterNodeJoin, info, cancellationToken: stoppingToken);
            }
            else
            {
                await _schedulerUtil.Stop(stoppingToken);
                throw new PlanarException("cluster health check fail. Could not join to cluster. See previous errors for more details");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(JoinToCluster));
            await _schedulerUtil.Stop(stoppingToken);
            await _schedulerUtil.Shutdown(stoppingToken);
            Shutdown();
        }
    }

    private async Task LoadGlobalConfigInner(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Initialize: {Operation}", nameof(LoadGlobalConfig));
            await LoadGlobalConfig(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(LoadGlobalConfig));
            Shutdown();
        }
    }

    private async Task LoadMonitorHooks()
    {
        try
        {
            _logger.LogInformation("Initialize: {Operation}", nameof(LoadMonitorHooks));

            using var scope = _serviceProvider.CreateScope();
            var monitorDomain = scope.ServiceProvider.GetRequiredService<MonitorDomain>();
            await monitorDomain.ReloadHooks(clusterReload: false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(LoadMonitorHooks));
        }
    }

    private void LogClustering()
    {
        if (AppSettings.Cluster.Clustering)
        {
            _logger.LogInformation("join to cluster [instance id: {Id}]", _schedulerUtil.SchedulerInstanceId);
        }
        else
        {
            _logger.LogInformation("non clustering instance");
        }
    }

    private void LogDeadNodes(List<ClusterNode> nodes)
    {
        if (nodes != null && nodes.Count != 0)
        {
            var text = string.Join(',', nodes.Select(n => $"{n.Server}:{n.Port}").ToArray());
            _logger.LogWarning("there are dead cluster nodes. Current node will not check health with them {Nodes}", text);
        }
    }

    private async Task RemoveSchedulerCluster()
    {
        var cluster = new ClusterNode
        {
            Server = Environment.MachineName,
            Port = AppSettings.General.ApiPort,
            InstanceId = _schedulerUtil.SchedulerInstanceId
        };

        var services = new ServiceCollection();
        services.AddPlanarDataLayerWithContext();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<IClusterData>();
        await dal.RemoveClusterNode(cluster);

        if (!AppSettings.Cluster.Clustering) { return; }

        // Monotoring
        var info = new MonitorSystemInfo("Cluster node removed from {{MachineName}}");
        info.MessagesParameters.Add("Port", AppSettings.General.ApiPort.ToString());
        info.MessagesParameters.Add("InstanceId", _schedulerUtil.SchedulerInstanceId);
        info.AddMachineName();
        SafeSystemScan(MonitorEvents.ClusterNodeRemoved, info);
    }

    #endregion Initialize Scheduler
}