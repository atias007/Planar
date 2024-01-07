using HelloWorldWithData;
using Planar.Job;

PlanarJob.Debugger.AddProfile("Incorrect Data", b => b.WithJobData("DurationSeconds", "abc"));

PlanarJob.Debugger.AddProfile("Data = 20", b => b.WithJobData("DurationSeconds", 20));

PlanarJob.Debugger.AddProfile("Speed Running", b => b.WithJobData("Speed Running", true));

PlanarJob.Start<Job>();