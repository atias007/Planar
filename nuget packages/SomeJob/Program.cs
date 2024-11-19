using Planar.Job;
using SomeJob;

PlanarJob.Start<Worker>();
PlanarJob.Debugger.AddProfile("Demo 1", builder =>
    builder
        .WithJobData("X", "1")
        .WithJobData("Z", "2")
        .WithGlobalSettings("RabbitMQ:Host", "127.0.0.1"));

PlanarJob.Debugger.AddProfile("Demo 100", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555));

PlanarJob.Debugger.AddProfile("Map Error", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedDate", "X"));

PlanarJob.Debugger.AddProfile("Demo With Date", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555)
        .WithExecutionDate(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)));

PlanarJob.Debugger.AddProfile("Demo With Recovery", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555)
        .SetRecoveringMode());

PlanarJob.Debugger.AddProfile("Override Global Settings", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555)
        .WithGlobalSettings("Max Diffrance Hours", 999));

PlanarJob.Start<Worker>();