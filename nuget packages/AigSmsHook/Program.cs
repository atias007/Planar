using Aig.Planar.SmsHook;

Planar.Hook.PlanarHook.Debugger.AddMonitorProfile("Test1", b => b
    .AddGlobalConfig("SmsHook:SourceSystem", "Test1")
    .AddGlobalConfig("SmsHook:CommonServicesUrl", "http://localhost")
    .AddTestUser());

Planar.Hook.PlanarHook.Start<Hook>();