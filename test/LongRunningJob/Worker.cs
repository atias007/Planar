using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Threading.Tasks;

namespace LongRunningJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            for (int i = 0; i < 130; i++)
            {
                UpdateProgress(i, 130);
                SetEffectedRows(i + 1);
                if (context.CancellationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("Cancel job");
                    break;
                }

                if (i % 10 == 0)
                {
                    Logger.LogInformation("Step {Index}", i);
                }

                await Task.Delay(1000);
            }
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}