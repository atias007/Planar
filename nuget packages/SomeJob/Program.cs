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

PlanarJob.Start<Worker>();