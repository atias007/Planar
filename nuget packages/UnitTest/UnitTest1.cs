using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;
using Planar.Job.Test;
using SomeJob;

namespace UnitTest
{
    public class Tests : BaseJobTest
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var run = JobRunner.ForJob<Worker>()
                .WithJobData("X", 10)
                .WithTriggerData("Y", 33)
                .WithExecutionDate(DateTime.Now.AddDays(-2))
                .WithGlobalSettings("Port", 1234);

            ExecuteJob(run).AssertSuccess();
        }
    }
}