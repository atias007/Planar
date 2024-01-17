// See https://aka.ms/new-console-template for more information
using Customs.SmsMonitorHook;
using Planar.Hook;

PlanarHook.Debugger.AddMonitorProfile("test me", b =>
    b.AddUsers(ub => ub.WithPhoneNumber1("0525805608")));

PlanarHook.Start<Hook>();