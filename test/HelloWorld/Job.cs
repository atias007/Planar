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
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var durationSeconds = new Random().Next(3, 40);
            Console.WriteLine($"Start execute job: {context.JobDetails.Key.Name}");
            Logger.LogInformation("Start execute job: {Name}", context.JobDetails.Key.Name);

            for (int i = 0; i < durationSeconds; i++)
            {
                Logger.LogInformation("Hello world: step {Iteration}", i);
                await Task.Delay(1000);
                UpdateProgress(i + 1, durationSeconds);
                IncreaseEffectedRows();
            }

            Logger.LogInformation("Finish");
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}