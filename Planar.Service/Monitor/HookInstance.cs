using Newtonsoft.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookInstance
    {
        public object Instance { get; set; }

        public MethodInfo Method { get; set; }

        public Task Handle(MonitorDetails details)
        {
            var users = JsonConvert.SerializeObject(details.Users);
            var group = JsonConvert.SerializeObject(details.Group);
            details.Users = null;
            details.Group = null;
            var json = JsonConvert.SerializeObject(details);

            var result = Method.Invoke(Instance, new object[] { json, users, group });
            return result as Task;
        }
    }
}