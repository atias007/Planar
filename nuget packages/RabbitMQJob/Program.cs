using Planar.Job;
using Planar.Job.RabbitMq;
using RabbitMQJob;

var connectionInfo = new RabbitMqJobStartPropertiesBuilder()
        .WithHostName("localhost")
        .AddJob<JobA>()
        .AddJob<JobB>()
        .Build();
await PlanarJob.StartAsync<JobA>(connectionInfo);