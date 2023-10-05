using Planar.Monitor.Hook;

namespace Planar.Hooks
{
    public class PlanarLogHook : BaseHook
    {
        public override string Name => "Planar.Log";

        public override Task Handle(IMonitorDetails monitorDetails)
        {
            return Task.CompletedTask;
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            return Task.CompletedTask;
        }
    }
}