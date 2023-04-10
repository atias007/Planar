using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookInstance
    {
        public const string HandleMethodName = "ExecuteHandle";
        public const string HandleSystemMethodName = "ExecuteHandleSystem";

        public object Instance { get; set; }

        public MethodInfo HandleMethod { get; set; }

        public MethodInfo HandleSystemMethod { get; set; }

        public Task Handle(MonitorDetails details, ILogger<MonitorUtil> logger)
        {
            if (HandleMethod == null)
            {
                throw new PlanarMonitorException($"method '{HandleMethodName}' could not be found in hook");
            }

            var messageBroker = new MonitorMessageBroker(logger, details);
            var result = HandleMethod.Invoke(Instance, new object[] { messageBroker });
            return (result as Task) ?? Task.CompletedTask;
        }

        public Task HandleSystem(MonitorSystemDetails details, ILogger<MonitorUtil> logger)
        {
            if (HandleSystemMethod == null)
            {
                throw new PlanarMonitorException($"method '{HandleMethodName}' could not be found in hook");
            }

            var messageBroker = new MonitorMessageBroker(logger, details);
            var result = HandleSystemMethod.Invoke(Instance, new object[] { messageBroker });
            return (result as Task) ?? Task.CompletedTask;
        }
    }
}