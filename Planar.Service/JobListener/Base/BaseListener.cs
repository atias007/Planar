using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Monitor;
using Quartz;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Planar.Service.List.Base
{
    public class BaseListener<T>
    {
        private readonly Singleton<ILogger<T>> _logger = new(GetLogger);

        private static ILogger<T> GetLogger()
        {
            return Global.ServiceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>;
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
                Logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
            }
        }

        protected void SafeLog(string source, Exception ex)
        {
            Logger.LogCritical(ex, "Error handle {Module}.{Source}: {Message}", typeof(T).Name, source, ex.Message);
        }

        public ILogger<T> Logger
        {
            get
            {
                return _logger.Instance;
            }
        }
    }
}