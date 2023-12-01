using Planar.Common.Exceptions;
using Planar.Monitor.Hook;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookWrapper
    {
        public const string ExecuteMethodName = "Execute";

        private HookWrapper()
        {
        }

        public static HookWrapper CreateInternal(BaseHook instance)
        {
            return new HookWrapper
            {
                HookType = HookTypeMembers.Internal,
                Name = instance.Name,
                Instance = instance,
                ExecuteMethod = SafeGetMethod(instance.Name, ExecuteMethodName, instance)
            };
        }

        public static HookWrapper CreateExternal(string filename)
        {
            return new HookWrapper
            {
                HookType = HookTypeMembers.External,
                Filename = filename
            };
        }

        internal enum HookTypeMembers
        {
            Internal,
            External
        }

        public MethodInfo? ExecuteMethod { get; private set; }

        public HookTypeMembers HookType { get; private set; }

        public BaseHook? Instance { get; private set; }

        public string? Filename { get; private set; }

        public string? Name { get; private set; }

        public Task Handle(MonitorDetails details)
        {
            if (ExecuteMethod == null)
            {
                throw new PlanarMonitorException($"method '{ExecuteMethod}' could not be found in hook '{Name}'");
            }

            var wrapper = new MonitorMessageWrapper(details);
            var json = JsonSerializer.Serialize(wrapper);
            return Task.Run(() =>
            {
                var result = ExecuteMethod.Invoke(Instance, new object[] { json });
                (result as Task)?.Wait();
            });
        }

        public Task HandleSystem(MonitorSystemDetails details, CancellationToken cancellationToken)
        {
            if (ExecuteMethod == null)
            {
                throw new PlanarMonitorException($"method '{ExecuteMethod}' could not be found in hook '{Name}'");
            }

            var wrapper = new MonitorMessageWrapper(details);
            var json = JsonSerializer.Serialize(wrapper);

            return Task.Run(() =>
            {
                var result = ExecuteMethod.Invoke(Instance, new object[] { json });
                (result as Task)?.Wait();
            }, cancellationToken);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "reflection base class with internal")]
        private static MethodInfo SafeGetMethod(string hookName, string methodName, object instance)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method ?? throw new PlanarException($"method {methodName} could not found in hook '{hookName}'");
        }
    }
}