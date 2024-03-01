using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace AbnormalExitCode;

internal class Job : BaseJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override Task ExecuteJob(IJobExecutionContext context)
    {
        var args = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        args.ToList().ForEach(async i =>
        {
            await Task.Delay(i * 1_000);
        });

        return Task.CompletedTask;
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
    }
}