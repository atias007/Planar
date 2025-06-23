using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace TestInvokeJobApi2;

internal class Job : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override Task ExecuteJob(IJobExecutionContext context)
    {
        Logger.LogInformation("Executing TestInvokeJobApi2 Job...");
        Logger.LogInformation("MergedJobDataMap:");
        foreach (var item in context.MergedJobDataMap)
        {
            Logger.LogInformation("Key: {Key}, Value: {Value}", item.Key, item.Value);
        }

        Logger.LogInformation("Now:");
        Logger.LogInformation(Now().ToString());

        Logger.LogInformation("Timeout Seconds:");
        Logger.LogInformation(context.TriggerDetails.Timeout.GetValueOrDefault().TotalSeconds.ToString("N2"));

        return Task.CompletedTask;
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}