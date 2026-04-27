using Planar.Job;
using Planar.Job.RabbitMQ;
using RabbitMQJob;

var connectionInfo = new RabbitMQConnectionInfo { Hostname = "localhost" };
await PlanarJob.StartAsync<Job>(connectionInfo);