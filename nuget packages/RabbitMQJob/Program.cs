using Planar.Job;
using Planar.Job.RabbitMQ;
using RabbitMQJob;

var connectionInfo = new RabbitMQJobStartProperties(planarHostName: "localhost");
await PlanarJob.StartAsync<Job>(connectionInfo);