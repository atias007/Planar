using Microsoft.Extensions.Logging;
using Planar.Common.Exceptions;
using Planar.Hooks;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    internal class HookWrapper
    {
        public const string ExecuteMethodName = "Execute";

        private HookWrapper(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public static HookWrapper CreateInternal(BaseSystemHook instance, ILogger logger)
        {
            var wrapper = new HookWrapper(instance.Name, instance.Description)
            {
                HookType = HookTypeMembers.Internal,
                Instance = instance,
                ExecuteMethod = SafeGetMethod(instance.Name, ExecuteMethodName, instance),
                Logger = logger
            };

            instance.SetLogger(logger);
            return wrapper;
        }

        public static HookWrapper CreateExternal(string filename, HookValidator validator, ILogger logger)
        {
            return new HookWrapper(validator.Name, validator.Description)
            {
                HookType = HookTypeMembers.External,
                Filename = filename,
                Logger = logger
            };
        }

        internal enum HookTypeMembers
        {
            Internal,
            External
        }

        public MethodInfo? ExecuteMethod { get; private set; }

        public HookTypeMembers HookType { get; private set; }

        public BaseSystemHook? Instance { get; private set; }

        public string? Filename { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public ILogger Logger { get; private set; } = null!;

        public Task Handle(MonitorDetails details, CancellationToken cancellationToken)
        {
            if (HookType == HookTypeMembers.External)
            {
                HookExecuter exe = null!;
                return Task.Run(() =>
                {
                    exe = new HookExecuter(Logger, Filename);
                    exe.HandleByExternalHook(details);
                }, cancellationToken).ContinueWith(t => exe.Dispose(), cancellationToken);
            }

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

        public Task HandleSystem(MonitorSystemDetails details, CancellationToken cancellationToken)
        {
            if (HookType == HookTypeMembers.External)
            {
                HookExecuter exe = null!;
                return Task.Run(() =>
                {
                    exe = new HookExecuter(Logger, Filename);
                    exe.HandleSystemByExternalHook(details);
                }, cancellationToken).ContinueWith(t => exe.Dispose(), cancellationToken);
            }

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