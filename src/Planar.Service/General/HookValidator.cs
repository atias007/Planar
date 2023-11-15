using Microsoft.Extensions.Logging;
using Planar.Monitor.Hook;

namespace Planar.Service.General
{
    internal sealed class HookValidator
    {
        public HookValidator(object? hook, ILogger logger)
        {
            if (hook == null)
            {
                logger.LogWarning("fail to load monitor hook. Hook instance is null");
                return;
            }

            var nameProp = hook.GetType().GetProperty(nameof(Name));
            if (nameProp != null)
            {
                Name = nameProp.GetValue(hook) as string ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = hook.GetType().Name;
            }

            var handleMethod = hook.GetType().GetMethod(nameof(BaseHook.Handle));
            if (handleMethod == null)
            {
                logger.LogWarning("fail to load monitor hook {Hook}. It does not have a handle method", hook.GetType().Name);
                return;
            }

            var handleSystemMethod = hook.GetType().GetMethod(nameof(BaseHook.HandleSystem));
            if (handleSystemMethod == null)
            {
                logger.LogWarning("fail to load monitor hook {Hook}. It does not have a handle system method", hook.GetType().Name);
                return;
            }

            IsValid = true;
        }

        public bool IsValid { get; private set; }

        public string Name { get; set; } = string.Empty;
    }
}