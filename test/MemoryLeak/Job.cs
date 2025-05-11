using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace MemoryLeak;

internal class Job : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        EffectedRows = 0;
        int i = 0;

        Logger.LogInformation("Job delay 10");
        await Task.Delay(10_000); // Simulate some work

        try
        {
            for (i = 0; i < 100_000; i++)
            {
                Logger.LogInformation("This is line number {Number} at {Date} for the memory leak test. Trying to crash planar process while running or when get the result in cli", i, DateTime.Now);
                EffectedRows++;
                //await UpdateProgressAsync(i + 1, 100_000);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in job: {Message}", ex.Message);
        }

        Logger.LogDebug("Job completed with {EffectedRows} rows affected.", i);
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}