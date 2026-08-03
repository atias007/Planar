using HelloWorld;
using Planar.Job;

PlanarJob.Debugger.AddProfile("Dev1", b => b.WithExecutionDate(DateTime.Now.AddMonths(-5)));
//var prop = new PlanarJobStartProperties { EncryptionKey = "tyZZrOD1R21YfCmu9cZRUyuqnKew7ikYJfA5NKTWsc4=" };
//await PlanarJob.StartAsync<Job>(prop);
await PlanarJob.StartAsync<Job>();