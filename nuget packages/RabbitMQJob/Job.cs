using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;

namespace RabbitMQJob;

internal class Job : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override Task ExecuteJob(IJobExecutionContext context)
    {
        return Task.CompletedTask;
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}