using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.API;
using Quartz;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

internal class ConfigReloadJob(IServiceScopeFactory scopeFactory, ILogger<ConfigReloadJob> logger) : SystemJob, IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await SafeDoWork(context);
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "system job for reload all config with source url";
        await ScheduleLowPriority<ConfigReloadJob>(scheduler, description, "0 0/20 * * * ?", stoppingToken: stoppingToken);
    }

    private async Task SafeDoWork(IJobExecutionContext context)
    {
        try
        {
            await DoWork();
            SafeSetLastRun(context, logger);
        }
        catch
        {
            // *** DO NOTHING *** //
        }
    }

    private async Task DoWork()
    {
        using var scope = scopeFactory.CreateScope();
        var configDomain = scope.ServiceProvider.GetRequiredService<ConfigDomain>();
        await configDomain.FlushWithReloadExternalSourceUrl();
    }
}