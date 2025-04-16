using WindowsServiceCheck;
using Planar.Job;

PlanarJob.Debugger.AddProfile("with keys", b => b.WithJobData("keys", "MyServiceName2"));

await PlanarJob.StartAsync<Job>();