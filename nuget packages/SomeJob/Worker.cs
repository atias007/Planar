using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace SomeJob
{
    public class Worker : BaseJob
    {
        public DateTime? SomeDate { get; set; }

        public int? SomeMappedInt { get; set; }

        public int SimpleInt { get; set; }

        [IgnoreDataMap]
        public string? IgnoreData { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var factory = ServiceProvider.GetRequiredService<IBaseJob>();
            var child = ServiceProvider.GetRequiredService<WorkerChild>();

            child.TestMe();

            factory.PutJobData("NoExists", 12345);

            Logger.LogWarning("Now: {Now}", Now());

            Logger.LogInformation("SomeMappedInt: {SomeMappedInt}", SomeMappedInt);

            SomeMappedInt = 1050;

            var maxDiffranceHours = Configuration.GetValue("Max Diffrance Hours", 12);
            Logger.LogInformation("Value is {Value} while default value is 12", maxDiffranceHours);
            AddAggregateException(new Exception("agg ex"));

            var exists = context.MergedJobDataMap.Exists("X");
            Logger.LogDebug("Data X Exists: {Value}", exists);

            exists = context.MergedJobDataMap.Exists("Y");
            Logger.LogDebug("Data Y Exists: {Value}", exists);

            var a = context.MergedJobDataMap.Get<int>("X");
            Logger.LogDebug("Data X Value: {Value}", a);
            a++;
            PutJobData("X", a);
            a = context.MergedJobDataMap.Get<int>("X");
            Logger.LogDebug("Data X Value: {Value}", a);

            var z = context.MergedJobDataMap.Get("Z");
            Logger.LogDebug("Data Z Value: {Value}", z);
            PutJobData("Z", "NewZData");
            Logger.LogInformation("Z=" + context.MergedJobDataMap.Get("Z"));

            var rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            IncreaseEffectedRows();
            rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            IncreaseEffectedRows();
            rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);

            Logger.LogWarning("JobRunTime: {JobRunTime}", JobRunTime);

            Logger.LogInformation("SomeDate: {SomeDate}", SomeDate);
            SomeDate = Now();

            UpdateProgress(66);

            SimpleInt += 5;
            IgnoreData = "x";

            var a1 = factory.Context.MergedJobDataMap.Get<int>("X");
            var b1 = factory.GetEffectedRows();
            Logger.LogInformation("Test IBaseJob: X={X}, EffectedRows={EffectedRows}", a1, b1);

            var no = factory.Context.MergedJobDataMap.Exists("NoExists");
            Logger.LogInformation("Test IBaseJob: Has NoExists? {NoExists}", no);

            await Task.CompletedTask;
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            services.AddTransient<WorkerChild>();
            services.AddTransient<GeneralUtil>();
        }
    }
}