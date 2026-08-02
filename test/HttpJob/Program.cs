using HttpJob;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Planar.Job.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((hostingContext, configuration) =>
{
    configuration.MinimumLevel.Debug()
        .WriteTo.Console();
});

var app = builder.Build();
app.UseSerilogRequestLogging();

var properties = new HttpJobStartPropertiesBuilder()
        .WithPlanarHostName("localhost")
        .WithHost(app)
        .AddJob<SomeJob>("DemoHttp")
        .Build();

await PlanarJob.StartAsync(properties);