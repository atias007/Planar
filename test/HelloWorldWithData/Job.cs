using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HelloWorldWithData
{
    internal class Job : BaseJob
    {
        [JobData]
        public int DurationSeconds { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            if (DurationSeconds == 0) { DurationSeconds = 5; }
            Logger.LogInformation("Start execute job: {Name}", context.JobDetails.Key.Name);
            Logger.LogInformation("DurationSeconds data: {DurationSeconds}", DurationSeconds);

            var isSpeed = context.JobDetails.JobDataMap.ContainsKey("Speed Running");
            if (isSpeed)
            {
                Logger.LogInformation("==>> Speed Running");
            }

            var delay = isSpeed ? 500 : 1000;

            EffectedRows = 0;
            for (int i = 0; i < DurationSeconds; i++)
            {
                Logger.LogInformation("Hello world with data: step {Iteration}", i);
                await Task.Delay(delay);
                UpdateProgress(i + 1, DurationSeconds);
                EffectedRows++;
            }

            DurationSeconds++;
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}