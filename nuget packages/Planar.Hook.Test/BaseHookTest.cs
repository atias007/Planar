using System;
using System.Threading.Tasks;

namespace Planar.Hook.Test
{
    public abstract class BaseHookTest
    {
        protected IMonitorDetailsBuilder CreateMonitorDetailsBuilder() => PlanarHook.Debugger.CreateMonitorDetailsBuilder();

        protected IMonitorSystemDetailsBuilder CreateMonitorSystemDetailsBuilder() => PlanarHook.Debugger.CreateMonitorSystemDetailsBuilder();

        protected async Task ExecuteMonitor<T>(Action<IMonitorDetailsBuilder> action)
           where T : BaseHook, new()
        {
            var builder = CreateMonitorDetailsBuilder();
            action(builder);
            var details = builder.Build();
            await ExecuteMonitor<T>(details);
        }

        protected async Task ExecuteMonitor<T>(IMonitorDetails monitorDetails)
           where T : BaseHook, new()
        {
            var instance = new T();
            await instance.Handle(monitorDetails);
        }

        protected async Task ExecuteSystemMonitor<T>(Action<IMonitorSystemDetailsBuilder> action)
           where T : BaseHook, new()
        {
            var builder = CreateMonitorSystemDetailsBuilder();
            action(builder);
            var details = builder.Build();
            await ExecuteSystemMonitor<T>(details);
        }

        protected async Task ExecuteSystemMonitor<T>(IMonitorSystemDetails monitorSystemDetails)
           where T : BaseHook, new()
        {
            var instance = new T();
            await instance.HandleSystem(monitorSystemDetails);
        }
    }
}