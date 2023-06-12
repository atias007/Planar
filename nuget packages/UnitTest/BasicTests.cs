using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;
using Planar.Job.Test;
using SomeJob;

namespace UnitTest
{
    public class BasicTests : BaseJobTest
    {
        protected override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        protected override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            services.AddTransient<WorkerChild>();
            services.AddTransient<GeneralUtil>();
        }

        [Test]
        public void GeneralTest()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("SomeDate", DateTime.Now)
                .WithJobData("SimpleInt", 44)
                .WithTriggerData("Y", 33)
                .WithTriggerData("SomeInt", null)
                .WithExecutionDate(DateTime.Now.AddDays(-2))
                .WithGlobalSettings("Port", 1234);

            var result = ExecuteJob(run);
            result.Assert.Status.Success()
                .EffectedRows.IsNotEmpty()
                .Data.ContainsKey("NoExists")
                .Data.Key("NoExists").EqualsTo(12345);
        }

        [Test]
        public void FailOnMissingData()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("Z", "SomeString")
                .WithJobData("SimpleInt", 44);

            var result = ExecuteJob(run);
            result.Assert.Status.Fail();
        }

        [Test]
        public void SimpleIntIncrease()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("SimpleInt", 44);

            var result = ExecuteJob(run);
            result.Assert.Status.Success()
                .Data.Key("SimpleInt").EqualsTo(49);
        }

        [Test]
        public void IgnoreData()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("IgnoreData", null);

            var result = ExecuteJob(run);
            result.Assert.Status.Success()
                .Data.Key("IgnoreData").IsNull();
        }

        [Test]
        public void CancelJob()
        {
            var run = ExecuteJobBuilder
                .CreateBuilderForJob<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .CancelJobAfterSeconds(5);

            var result = ExecuteJob(run);
            result.Assert.Status.Fail();
            Assert.That(result.IsCanceled, Is.True);
        }
    }
}