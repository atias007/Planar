using Planar.Job;
using Planar.Job.RabbitMq;
using RabbitMQJob;

var connectionInfo = new RabbitMqJobStartPropertiesBuilder()
        .WithHostName("localhost")
        .AddJob<JobA>()
        .AddJob<JobB>()
        .Build();

PlanarJob.Debugger.AddProfile("Test Profile", builder =>
{
    builder.ForJob<JobB>();
});

await PlanarJob.StartAsync<JobA>(connectionInfo);