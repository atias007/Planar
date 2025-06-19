using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Watcher;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/planar.watcher.log", rollingInterval: RollingInterval.Month)
            .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Planar.Watcher";
});

builder.Services.AddHostedService<WatcherService>();
builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog();
});

try
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}

Log.CloseAndFlush();