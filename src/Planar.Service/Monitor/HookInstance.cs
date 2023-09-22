using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookInstance
    {
        public const string HandleMethodName = "ExecuteHandle";
        public const string HandleSystemMethodName = "ExecuteHandleSystem";

        public object? Instance { get; set; }

        public MethodInfo? HandleMethod { get; set; }

        public MethodInfo? HandleSystemMethod { get; set; }

        public Task Handle(MonitorDetails details, ILogger<MonitorUtil> logger)
        {
            if (HandleMethod == null)
            {
                throw new PlanarMonitorException($"method '{HandleMethodName}' could not be found in hook");
            }

            var messageBroker = new MonitorMessageBroker(logger, details);
            return Task.Run(() =>
            {
                var result = HandleMethod.Invoke(Instance, new object[] { messageBroker });
                (result as Task)?.Wait();
            });
        }

        public Task HandleSystem(MonitorSystemDetails details, ILogger<MonitorUtil> logger, CancellationToken cancellationToken = default)
        {
            if (HandleSystemMethod == null)
            {
                throw new PlanarMonitorException($"method '{HandleMethodName}' could not be found in hook");
            }

            var messageBroker = new MonitorMessageBroker(logger, details);

            return Task.Run(() =>
            {
                var result = HandleSystemMethod.Invoke(Instance, new object[] { messageBroker });
                (result as Task)?.Wait();
            }, cancellationToken);
        }
    }
}