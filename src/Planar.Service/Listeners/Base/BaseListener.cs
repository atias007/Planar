﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Planar.Service.Listeners.Base;

public abstract class BaseListener<T>(IServiceScopeFactory serviceScopeFactory, ILogger logger)
    where T : class
{
    protected readonly ILogger _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    protected IServiceScopeFactory ServiceScopeFactory => _serviceScopeFactory;

    protected void SafeSystemScan(MonitorEvents @event, MonitorSystemInfo details, Exception? exception = default)
    {
        MonitorUtil.SafeSystemScan(_serviceScopeFactory, _logger, @event, details, exception);
    }

    protected void SafeScan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = default)
    {
        try
        {
            if (JobKeyHelper.IsSystemJobKey(context.JobDetail.Key)) { return; }
            if (MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

            using var scope = _serviceScopeFactory.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
            monitor.Scan(@event, context, exception);
        }
        catch (ObjectDisposedException)
        {
            ServiceUtil.AddDisposeWarningToLog(_logger);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeScan);
            _logger.LogCritical(ex, "Error handle {Source}: {Message}", source, ex.Message);
        }
    }

    protected void LogCritical(string source, Exception ex)
    {
        _logger.LogCritical(ex, "Error handle {Module}.{Source}: {Message}", typeof(T).Name, source, ex.Message);
    }

    #region Execute Data Layer

    protected async Task ExecuteDal<TDataLayer>(Func<TDataLayer, Task> func)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            await func.Invoke(dal);
        }
        catch (ObjectDisposedException)
        {
            await ExecuteDalOnObjectDisposedException(func);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDal));
        }
    }

    protected async Task<TResponse?> ExecuteDal<TDataLayer, TResponse>(Expression<Func<TDataLayer, Task<TResponse>>> exp)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            return await exp.Compile().Invoke(dal);
        }
        catch (ObjectDisposedException)
        {
            return await ExecuteDalOnObjectDisposedException(exp);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDal));
            return default;
        }
    }

    protected void ExecuteDal<TDataLayer>(Expression<Action<TDataLayer>> exp)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            exp.Compile().Invoke(dal);
        }
        catch (ObjectDisposedException)
        {
            ExecuteDalOnObjectDisposedException(exp);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDal));
        }
    }

    private async Task ExecuteDalOnObjectDisposedException<TDataLayer>(Func<TDataLayer, Task> func)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            var services = new ServiceCollection();
            services.AddPlanarDataLayerWithContext();
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            await func.Invoke(dal);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDalOnObjectDisposedException));
        }
    }

    private async Task<TResponse?> ExecuteDalOnObjectDisposedException<TDataLayer, TResponse>(Expression<Func<TDataLayer, Task<TResponse>>> exp)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            var services = new ServiceCollection();
            services.AddPlanarDataLayerWithContext();
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            return await exp.Compile().Invoke(dal);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDalOnObjectDisposedException));
            return default;
        }
    }

    private void ExecuteDalOnObjectDisposedException<TDataLayer>(Expression<Action<TDataLayer>> exp)
        where TDataLayer : IBaseDataLayer
    {
        try
        {
            var services = new ServiceCollection();
            services.AddPlanarDataLayerWithContext();
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            exp.Compile().Invoke(dal);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialize/Execute DataLayer at {MethodName}", nameof(ExecuteDalOnObjectDisposedException));
        }
    }

    #endregion Execute Data Layer

    protected static bool IsSystemJobKey(JobKey jobKey)
    {
        return JobKeyHelper.IsSystemJobKey(jobKey);
    }

    protected static bool IsSystemTriggerKey(TriggerKey triggerKey)
    {
        return TriggerHelper.IsSystemTriggerKey(triggerKey);
    }

    protected static bool IsSystemJob(IJobDetail job)
    {
        return JobKeyHelper.IsSystemJobKey(job.Key);
    }

    protected static bool IsSystemTrigger(ITrigger trigger)
    {
        return TriggerHelper.IsSystemTriggerKey(trigger.Key);
    }
}