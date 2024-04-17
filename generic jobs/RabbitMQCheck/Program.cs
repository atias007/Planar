using Planar.Job;
using RabbitMQCheck;

PlanarJob.Debugger.AddProfile("test1", b => b.WithJobData($"last.fail.demo.consumers", DateTimeOffset.UtcNow.AddSeconds(-3)));

PlanarJob.Start<Job>();