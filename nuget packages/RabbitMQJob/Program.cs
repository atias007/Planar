using Planar.Job;
using Planar.Job.RabbitMq;
using RabbitMQJob;

var connectionInfo = new RabbitMqJobStartPropertiesBuilder()
        .WithPlanarHostName("localhost")
        .WithRabbitMqConnectionFactory(new RabbitMQ.Client.ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            VirtualHost = "Planar",
            Port = 5672
        })
        .AddJob<JobA>()
        .AddJob<JobB>()
        .Build();

PlanarJob.Debugger.AddProfile("Test Profile", builder =>
{
    builder.ForJob<JobB>();
});

await PlanarJob.StartAsync(connectionInfo);