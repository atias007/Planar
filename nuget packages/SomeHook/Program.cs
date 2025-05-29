using Planar.Hook;
using SomeHook;

////PlanarHook.Debugger.AddMonitorProfile("Test Monitor 1", b => b
////    .AddDataMap("x", "y")
////    .AddGlobalConfig("A1", "B1")
////    .SetDurable()
////    .SetRecovering()
////    .WithEnvironment("UnitTest"));

////PlanarHook.Debugger.AddMonitorSystemProfile("Test System Monitor 1", b => b
////    .AddGlobalConfig("A1", "B1")
////    .WithEnvironment("UnitTest")
////    .WithEventId(10001)
////    .WithEventTitle("No Exists Title")
////    .AddMessageParameter("p1", "v1")
////    .WithMessage("Hiiii... Test Message")
////    );

await PlanarHook.StartAsync<TestHook>();