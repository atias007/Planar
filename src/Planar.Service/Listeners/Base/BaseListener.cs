using Microsoft.Extensions.DependencyInjection;
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

namespace Planar.Service.Listeners.Base
{
    public abstract class BaseListener<T>
    {
        protected readonly ILogger<T> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        protected BaseListener(IServiceScopeFactory serviceScopeFactory, ILogger<T> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected IServiceScopeFactory ServiceScopeFactory => _serviceScopeFactory;

        protected async Task SafeSystemScan(MonitorEvents @event, MonitorSystemInfo details, Exception? exception = default)
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

        protected async Task SafeScan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = default)
        {
            try
            {
                if (MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<IMonitorUtil>();
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

        #region Execute Data Layer

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

        protected async Task<TResponse> ExecuteDal<TDataLayer, TResponse>(Expression<Func<TDataLayer, Task<TResponse>>> exp)
            where TDataLayer : BaseDataLayer
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
                _logger.LogCritical(ex, $"Error initialize/Execute DataLayer at {nameof(BaseListener<T>)}");
                throw;
            }
        }

        protected void ExecuteDal<TDataLayer>(Expression<Action<TDataLayer>> exp)
            where TDataLayer : BaseDataLayer
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

        private async Task<TResponse> ExecuteDalOnObjectDisposedException<TDataLayer, TResponse>(Expression<Func<TDataLayer, Task<TResponse>>> exp)
            where TDataLayer : BaseDataLayer
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
                _logger.LogCritical(ex, $"Error initialize/Execute DataLayer at {nameof(BaseListener<T>)}");
                throw;
            }
        }

        private void ExecuteDalOnObjectDisposedException<TDataLayer>(Expression<Action<TDataLayer>> exp)
            where TDataLayer : BaseDataLayer
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
                _logger.LogCritical(ex, $"Error initialize/Execute DataLayer at {nameof(BaseListener<T>)}");
                throw;
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

        protected bool IsSystemJob(IJobDetail job)
        {
            return JobKeyHelper.IsSystemJobKey(job.Key);
        }

        protected bool IsSystemTrigger(ITrigger trigger)
        {
            return TriggerHelper.IsSystemTriggerKey(trigger.Key);
        }
    }
}