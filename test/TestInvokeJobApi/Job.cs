using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace TestInvokeJobApi;

internal class Job : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        Logger.LogInformation("Executing TestInvokeJobApi Job...");
        Logger.LogInformation("Wait 10 seconds...");
        await Task.Delay(10_000);
        Logger.LogInformation("Invoke hello world with data");
        await base.InvokeJobAsync("Demo.HelloWorld With Data");
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}