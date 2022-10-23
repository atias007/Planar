using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookInstance
    {
        public object Instance { get; set; }

        public MethodInfo Method { get; set; }

        public Task Handle(MonitorDetails details, ILogger<MonitorUtil> logger)
        {
            var messageBroker = new MonitorMessageBroker(logger, details);
            var result = Method.Invoke(Instance, new object[] { messageBroker });
            return result as Task;
        }
    }
}