using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planar.Job;
using Planar.Job.RabbitMq;
using RabbitMQJob;

var builder = new HostApplicationBuilder();
builder.Services.AddSingleton<DemoSignleton>();
var app = builder.Build();

var connectionInfo = new RabbitMqJobStartPropertiesBuilder()
        .WithPlanarHostName("localhost")
        .WithHost(app)
        .AddHostSingletonType<DemoSignleton>()
        .WithDeadLetterExchange("DLX")
        .WithDeadLetterRoutingKey("Errors")
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