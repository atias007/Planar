using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job.Test;
using SomeJob;

namespace UnitTest
{
    public class Tests : BaseJobTest
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, string environment)
        {
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var log = ExecuteJob<Worker>();
            Assert.That(log.Status, Is.EqualTo(0));
        }
    }
}