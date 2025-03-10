﻿using HealthCheck;
using Planar.Job;

PlanarJob.Debugger.AddProfile("With Fail Count", job =>
{
    job
        .WithJobData("fail.count.http://localhost", "5")
        .WithJobData("fail.count.http://127.0.0.1", "5")
        .WithJobData("fail.count.http://localhost:5341/", "7");
});

PlanarJob.Debugger.AddProfile("With Trigger Name 'every-1-hour'", job =>
{
    job
    .WithTriggerName("every-1-hour");
});

await PlanarJob.StartAsync<Job>();