using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Monitor;
using Polly;
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

        protected async Task SafeSystemScan(MonitorEvents @event, MonitorSystemInfo details, Exception exception = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetService<MonitorUtil>();
                await monitor.Scan(@event, details, exception, cancellationToken);
            }
            catch (Exception ex)
            {
                var source = nameof(SafeSystemScan);
                _logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetService<MonitorUtil>();
                await monitor.Scan(@event, details, exception, cancellationToken);
            }
        }

        protected async Task SafeScan(MonitorEvents @event, IJobExecutionContext context, Exception exception = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceScopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetService<MonitorUtil>();
                await monitor.Scan(@event, context, exception, cancellationToken);
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
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error initialize/Execute DataLayer at BaseJobListenerWithDataLayer");
                throw;
            }
        }
    }
}