using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;

namespace CircuitBreakerTester
{
    internal class Job : BaseJob
    {
        [JobData]
        public int? Counter { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public async override Task ExecuteJob(IJobExecutionContext context)
        {
            await Task.Yield();
            Counter = Counter.GetValueOrDefault() + 1;
            if (Counter % 7 != 0)
            {
                throw new Exception("Counter is not divisible by 7");
            }
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}