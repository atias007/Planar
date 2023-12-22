using System;
using System.Threading.Tasks;

namespace Planar.Hook.Test
{
    public abstract class BaseHookTest
    {
        protected IMonitorDetailsBuilder CreateMonitorDetailsBuilder() => PlanarHook.Debugger.CreateMonitorDetailsBuilder();

        protected IMonitorSystemDetailsBuilder CreateMonitorSystemDetailsBuilder() => PlanarHook.Debugger.CreateMonitorSystemDetailsBuilder();

        protected async Task ExecuteMinitor<T>(Action<IMonitorDetailsBuilder> action)
           where T : BaseHook, new()
        {
            var builder = CreateMonitorDetailsBuilder();
            action(builder);
            var details = builder.Build();
            await ExecuteMinitor<T>(details);
        }

        protected async Task ExecuteMinitor<T>(IMonitorDetails monitorDetails)
           where T : BaseHook, new()
        {
            var instance = new T();
            await instance.Handle(monitorDetails);
        }

        protected async Task ExecuteSystemMinitor<T>(Action<IMonitorSystemDetailsBuilder> action)
           where T : BaseHook, new()
        {
            var builder = CreateMonitorSystemDetailsBuilder();
            action(builder);
            var details = builder.Build();
            await ExecuteSystemMinitor<T>(details);
        }

        protected async Task ExecuteSystemMinitor<T>(IMonitorSystemDetails monitorSystemDetails)
           where T : BaseHook, new()
        {
            var instance = new T();
            await instance.HandleSystem(monitorSystemDetails);
        }
    }
}