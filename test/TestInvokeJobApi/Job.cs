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
        Logger.LogInformation("Invoke TestInvokeJobApi2");
        await InvokeJobAsync("Demo.TestInvokeJobApi2", new InvokeJobOptions
        {
            Timeout = TimeSpan.FromSeconds(111),
            Data = new Dictionary<string, string?> { { "Key1", "Value1" }, { "Key2", "Value2" } },
            NowOverrideValue = DateTime.Now.Date.AddHours(1).AddMinutes(1).AddSeconds(1).AddMilliseconds(1),
            MaxRetries = 5,
            RetrySpan = TimeSpan.FromSeconds(30)
        });
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}