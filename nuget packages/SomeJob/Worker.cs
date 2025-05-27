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

        public DateTime SomeMappedDate { get; set; }

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

            await factory.PutJobDataAsync("NoExists", 12345);

            Logger.LogWarning("Now: {Now:ddd MMM yyyy}", Now());

            Logger.LogInformation("SomeMappedInt: {SomeMappedInt}", SomeMappedInt);

            SomeMappedInt++;

            var maxDiffranceHours = Configuration.GetValue("Max Diffrance Hours", 12);
            Logger.LogInformation("Configuration: Max Diffrance Hours={Value} while default is 12", maxDiffranceHours);

            var someSettings = Configuration.GetValue<string>("Some New Settings");
            Logger.LogInformation("Configuration: Some New Settings={Value}", someSettings);

            await AddAggregateExceptionAsync(new Exception("agg ex"));

            var exists = context.MergedJobDataMap.Exists("X");
            Logger.LogDebug("Data X Exists: {Value}", exists);

            exists = context.MergedJobDataMap.Exists("Y");
            Logger.LogDebug("Data Y Exists: {Value}", exists);

            var a = context.MergedJobDataMap.Get<int>("X");
            Logger.LogDebug("Data X Value: {Value}", a);
            a++;
            await PutJobDataAsync("X", a);

            var z = context.MergedJobDataMap.Get("Z");
            Logger.LogDebug("Data Z Value: {Value}", z);
            await PutJobDataAsync("Z", "NewZData");

            var rows = EffectedRows;
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            await SetEffectedRowsAsync(0);
            rows = EffectedRows;
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            await IncreaseEffectedRowsAsync();
            rows = EffectedRows;
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            Logger.LogWarning("JobRunTime: {JobRunTime}", JobRunTime);
            Logger.LogInformation("SomeDate: {SomeDate}", SomeDate);
            SomeDate = Now();
            Logger.LogWarning("Recovery: {Recovery}", context.Recovering);
            await UpdateProgressAsync(66);

            SimpleInt += 5;
            IgnoreData = "x";

            var a1 = factory.Context.MergedJobDataMap.Get<int>("X");
            var b1 = factory.EffectedRows;
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