using HealthCheck;
using Planar.Job;

PlanarJob.Debugger.AddProfile("With Fail Count", job =>
{
    job
        .WithJobData("fail.count_https://github.com", "5");
});

PlanarJob.Start<Job>();