using Planar.Monitor.Hook;
using Planar.MonitorHook.MessageBirdSms;

PlanarHook.Debugger.AddMonitorProfile("Debug1", b => b
    .AddGlobalConfig("MessageBirdCountryCode", "972")
    .AddGlobalConfig("MessageBirdSmsAccessKey", "5xP2Nka0ihjnDCud1cmo1xPFe")
    .AddGlobalConfig("MessageBirdSmsFrom", "0525805608")
    .AddUsers(ub => ub
        .WithPhoneNumber1("0525805608")
        ));

PlanarHook.Start<MessageBirdSmsHook>();