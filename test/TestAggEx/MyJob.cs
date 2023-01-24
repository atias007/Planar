using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using Planar.Job;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestAggEx
{
    public class MyJob : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, string environment)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            for (int i = 0; i < 6; i++)
            {
                Thread.Sleep(1000);
                try
                {
                    if (i % 3 == 0)
                    {
                        throw new PlanarJobException($"This is demo exception occur at loop no. {i}");
                    }

                    Logger.LogInformation("This is loop no. {Index}", i);

                    IncreaseEffectedRows();
                }
                catch (Exception ex)
                {
                    AddAggragateException(ex);
                }
                finally
                {
                    UpdateProgress(i + 1, 10);
                }
            }

            CheckAggragateException();

            return Task.CompletedTask;
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
        }
    }
}