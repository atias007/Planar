using Planar.Hook;

namespace SomeHook
{
    internal class TestHook : BaseHook
    {
        public override string Name => nameof(TestHook);

        public override Task Handle(IMonitorDetails monitorDetails)
        {
            Console.WriteLine("Handle");
            return Task.CompletedTask;
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            Console.WriteLine("HandleSystem");

            return Task.CompletedTask;
        }
    }
}