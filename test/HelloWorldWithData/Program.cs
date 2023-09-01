using HelloWorldWithData;
using Planar.Job;

PlanarJob.Debugger.AddProfile<Job>("Incorrect Data", b => b.WithJobData("DurationSeconds", "abc"));

PlanarJob.Debugger.AddProfile<Job>("Data = 20", b => b.WithJobData("DurationSeconds", 20));

PlanarJob.Start<Job>();