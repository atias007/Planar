using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Planar.Watcher;

internal class WatcherService(ILogger<WatcherService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Console.Out.WriteLineAsync("----------------------------------");
        await Console.Out.WriteLineAsync("- Planar watcher service started -");
        await Console.Out.WriteLineAsync("----------------------------------");
        logger.LogInformation("Planar watcher service started with interval {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            Console.WriteLine("x");
        }
    }
}