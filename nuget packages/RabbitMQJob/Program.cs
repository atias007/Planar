using Planar.Job;
using Planar.Job.RabbitMQ;
using RabbitMQJob;

var connectionInfo = new RabbitMQJobStartProperties("localhost1") { Hostname = "localhost" };
await PlanarJob.StartAsync<Job>(connectionInfo);