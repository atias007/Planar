using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;
using Planar.Job.Test;
using SomeJob;

namespace UnitTest
{
    public class Tests : BaseJobTest
    {
        protected override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        protected override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("X", 10)
                .WithTriggerData("Y", 33)
                .WithExecutionDate(DateTime.Now.AddDays(-2))
                .WithGlobalSettings("Port", 1234);

            ExecuteJob(run).AssertSuccess();
        }
    }
}