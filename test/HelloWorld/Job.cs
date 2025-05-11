using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HelloWorld
{
    internal class Job : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
            Version = new Version(5, 0);
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var now = Now();
            Logger.LogInformation("Now: {Now:dd/MM/yyyy HH:mm:ss}", now);
            Logger.LogInformation("Timeout: {Timeout}", context.TriggerDetails.Timeout);
            var durationSeconds = new Random().Next(10, 20);
            Logger.LogInformation("Start execute job: {Name}", context.JobDetails.Key.Name);
            await SetEffectedRowsAsync(0);
            for (int i = 0; i < durationSeconds; i++)
            {
                Logger.LogInformation("Hello world: step {Iteration}", i);
                await Task.Delay(1000);
                UpdateProgress(i + 1, durationSeconds);
                await IncreaseEffectedRowsAsync();
            }

            Logger.LogInformation("Finish");
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}