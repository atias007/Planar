using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners.Base
{
    public class BaseListener<T>
    {
        protected readonly ILogger<T> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BaseListener(IServiceScopeFactory serviceScopeFactory, ILogger<T> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected async Task SafeSystemScan(MonitorEvents @event, MonitorSystemInfo details, Exception exception = default)
        {
            try
            {
                if (!MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
                await monitor.Scan(@event, details, exception);
            }
            catch (ObjectDisposedException)
            {
                ServiceUtil.AddDisposeWarningToLog(_logger);
            }
            catch (Exception ex)
            {
                var source = nameof(SafeSystemScan);
                _logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
            }
        }

        protected async Task SafeScan(MonitorEvents @event, IJobExecutionContext context, Exception exception = default)
        {
            try
            {
                if (MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
                await monitor.Scan(@event, context, exception);
            }
            catch (ObjectDisposedException)
            {
                ServiceUtil.AddDisposeWarningToLog(_logger);
            }
            catch (Exception ex)
            {
                var source = nameof(SafeScan);
                _logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
            }
        }

        protected void LogCritical(string source, Exception ex)
        {
            _logger.LogCritical(ex, "Error handle {Module}.{Source}: {Message}", typeof(T).Name, source, ex.Message);
        }

        protected async Task ExecuteDal<TDataLayer>(Expression<Func<TDataLayer, Task>> exp)
            where TDataLayer : BaseDataLayer
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
                await exp.Compile().Invoke(dal);
            }
            catch (ObjectDisposedException)
            {
                await ExecuteDalOnObjectDisposedException(exp);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Error initialize/Execute DataLayer at {nameof(BaseListener<T>)}");
                throw;
            }
        }

        private async Task ExecuteDalOnObjectDisposedException<TDataLayer>(Expression<Func<TDataLayer, Task>> exp)
            where TDataLayer : BaseDataLayer
        {
            try
            {
                var services = new ServiceCollection();
                services.AddPlanarDataLayerWithContext();
                var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dal = scope.ServiceProvider.GetRequiredService<TDataLayer>();
                await exp.Compile().Invoke(dal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Error initialize/Execute DataLayer at {nameof(BaseListener<T>)}");
                throw;
            }
        }

        protected static bool IsSystemJobKey(JobKey jobKey)
        {
            return JobKeyHelper.IsSystemJobKey(jobKey);
        }

        protected static bool IsSystemTriggerKey(TriggerKey triggerKey)
        {
            return TriggerKeyHelper.IsSystemTriggerKey(triggerKey);
        }

        protected bool IsSystemJob(IJobDetail job)
        {
            return JobKeyHelper.IsSystemJobKey(job.Key);
        }

        protected bool IsSystemTrigger(ITrigger trigger)
        {
            return TriggerKeyHelper.IsSystemTriggerKey(trigger.Key);
        }
    }
}