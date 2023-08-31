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
            var run = CreateJobPropertiesBuilder<Worker>()
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
                .EffectedRows.IsNotEmpty();
        }

        [Test]
        public void FailOnMissingData()
        {
            var run = CreateJobPropertiesBuilder<Worker>()
                .WithJobData("Z", "SomeString")
                .WithJobData("SimpleInt", 44);

            var result = ExecuteJob(run);
            result.Assert.Status.Fail();
        }

        [Test]
        public void SimpleIntIncrease()
        {
            var run = CreateJobPropertiesBuilder<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("SimpleInt", 44);

            var result = ExecuteJob(run);
            result.Assert.Status.Success();
        }

        [Test]
        public void IgnoreData()
        {
            var run = CreateJobPropertiesBuilder<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("IgnoreData", null);

            var result = ExecuteJob(run);
            result.Assert.Status.Success();
        }

        [Test]
        public void MapError()
        {
            var run = CreateJobPropertiesBuilder<Worker>()
                .WithJobData("X", 10)
                .WithJobData("Z", "SomeString")
                .WithJobData("SomeMappedDate", null);

            var result = ExecuteJob(run);
            result.Assert.Status.Success();
        }
    }
}