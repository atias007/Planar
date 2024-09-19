using Planar.Job;
using RabbitMQCheck;
using System.Globalization;

PlanarJob.Debugger.AddProfile("test1", b => b.WithJobData("last.fail.demo", DateTimeOffset.UtcNow.AddSeconds(-13).ToString(CultureInfo.CurrentCulture)));
PlanarJob.Debugger.AddProfile("test2", b => b.WithJobData("last.fail.demo", DateTimeOffset.UtcNow.AddSeconds(-3).ToString(CultureInfo.CurrentCulture)));
PlanarJob.Debugger.AddProfile("test3", b => b
    .WithJobData("fail.count.demo.consumers", 10)
    .WithJobData("last.fail.demo.consumers", DateTimeOffset.UtcNow.AddSeconds(-13).ToString(CultureInfo.CurrentCulture)));

await PlanarJob.StartAsync<Job>();