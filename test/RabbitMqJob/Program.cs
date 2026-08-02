using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Planar.Job.RabbitMq;
using RabbitMQJob;

var builder = new HostApplicationBuilder();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddSingleton<DemoSignleton>();
var app = builder.Build();

#pragma warning disable S2068 // Credentials should not be hard-coded
var connectionInfo = new RabbitMqJobStartPropertiesBuilder()
        .WithPlanarHostName("localhost")
        .WithHost(app)
        .AddHostSingletonType<DemoSignleton>()
        .WithEncryptionKey("tyZZrOD1R21YfCmu9cZRUyuqnKew7ikYJfA5NKTWsc4=")
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
#pragma warning restore S2068 // Credentials should not be hard-coded

PlanarJob.Debugger.AddProfile("Test Profile", builder =>
{
    builder.ForJob<JobB>();
});

await PlanarJob.StartAsync(connectionInfo);