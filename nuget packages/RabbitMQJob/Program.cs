using Planar.Job;
using Planar.Job.RabbitMQ;
using RabbitMQJob;

try
{
    var connectionInfo = new RabbitMQConnectionInfo { Hostname = "localhost" };
    await PlanarJob.StartAsync<Job>(connectionInfo);
}
catch (Exception)
{
    throw;
}