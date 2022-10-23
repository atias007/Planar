using Microsoft.Extensions.Logging;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.List.Base
{
    public class BaseListener<T>
    {
        protected readonly ILogger<T> _logger;

        public BaseListener(ILogger<T> logger)
        {
            _logger = logger;
        }

        protected async Task SafeScan(MonitorEvents @event, IJobExecutionContext context, Exception exception = default, CancellationToken cancellationToken = default)
        {
            try
            {
                await MonitorUtil.Scan(MonitorEvents.ExecutionVetoed, context, null, cancellationToken);
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
    }
}