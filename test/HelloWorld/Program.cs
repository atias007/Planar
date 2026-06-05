using HelloWorld;
using Planar.Job;

PlanarJob.Debugger.AddProfile("Dev1", b => b.WithExecutionDate(DateTime.Now.AddMonths(-5)));

await PlanarJob.StartAsync<Job>();