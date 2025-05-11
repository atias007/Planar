using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace AggregateException
{
    internal class Job : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            await SetEffectedRowsAsync(0);

            for (int i = 0; i < 10; i++)
            {
                Logger.LogInformation("AggregateException: step {Iteration}", i);
                await Task.Delay(1000);

                var ex = new InvalidProgramException($"Invalid program exception {i}");
                AddAggregateException(ex, 60);

                await IncreaseEffectedRowsAsync();
                UpdateProgress(i + 1, 10);
            }

            CheckAggragateException();
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}