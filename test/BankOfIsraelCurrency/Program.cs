using BankOfIsraelCurrency;
using Planar.Job;

PlanarJob.Debugger.AddProfile<CurrencyLoader>("Dev 1", b => b.WithGlobalSettings("x", 3).WithRefireCount(3).SetRecoveringMode());
PlanarJob.Debugger.AddProfile<CurrencyLoader>("Dev 2", b => b.WithGlobalSettings("x", 3).WithRefireCount(3));

PlanarJob.Start<CurrencyLoader>();