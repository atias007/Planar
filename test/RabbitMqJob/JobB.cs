using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Planar.Job.Attributes;

namespace RabbitMQJob;

[JobRoutingKey("job-b")]
internal class JobB : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        for (var i = 0; i < 50; i++)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                Logger.LogCritical("Cancel!!!!");
                break;
            }
            Logger.LogInformation("N: {I}", i);
            await Task.Delay(500, context.CancellationToken);
        }
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddScoped<DataLayer>();
    }
}