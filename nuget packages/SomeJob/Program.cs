using Planar.Job;
using SomeJob;

PlanarJob.Debugger.AddProfile<Worker>("Demo 1", builder =>
    builder
        .WithJobData("X", "1")
        .WithJobData("Z", "2"));

PlanarJob.Debugger.AddProfile<Worker>("Demo 100", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555));

PlanarJob.Debugger.AddProfile<Worker>("Demo With Date", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555)
        .WithExecutionDate(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)));

PlanarJob.Debugger.AddProfile<Worker>("Demo With Recovery", builder =>
    builder
        .WithJobData("X", "100")
        .WithJobData("Z", "200")
        .WithJobData("SomeMappedInt", 555)
        .SetRecoveringMode());

PlanarJob.Start<Worker>();