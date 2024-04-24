using HealthCheck;
using Planar.Job;

PlanarJob.Debugger.AddProfile("With Fail Count", job =>
{
    job
        .WithJobData("fail.count.http://localhost", "5")
        .WithJobData("fail.count.http://127.0.0.1", "5");
});

PlanarJob.Start<Job>();