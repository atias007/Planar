using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace SomeJob
{
    public class Worker : BaseJob
    {
        public DateTime? SomeDate { get; set; }

        public int? SomeInt { get; set; }

        public int SimpleInt { get; set; }

        [IgnoreDataMap]
        public string? IgnoreData { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var fact = ServiceProvider.GetRequiredService<IBaseJob>();
            var child = ServiceProvider.GetRequiredService<WorkerChild>();

            fact.PutJobData("NoExists", 12345);

            Logger.LogWarning("Now: {Now}", Now());

            var maxDiffranceHours = Configuration.GetValue("Max Diffrance Hours", 12);
            Logger.LogInformation("Value is {Value} while default value is 12", maxDiffranceHours);
            AddAggregateException(new Exception("agg ex"));

            var exists = IsDataExists("X");
            Logger.LogDebug("Data X Exists: {Value}", exists);

            exists = IsDataExists("Y");
            Logger.LogDebug("Data Y Exists: {Value}", exists);

            var a = GetData<int>("X");
            Logger.LogDebug("Data X Value: {Value}", a);
            a++;
            PutJobData("X", a);
            a = GetData<int>("X");
            Logger.LogDebug("Data X Value: {Value}", a);

            var rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            IncreaseEffectedRows();
            rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);
            IncreaseEffectedRows();
            rows = GetEffectedRows();
            Logger.LogDebug("GetEffectedRows: {Value}", rows);

            await Task.Delay(10000, context.CancellationToken);
            FailOnStopRequest();
            Logger.LogWarning("JobRunTime: {JobRunTime}", JobRunTime);

            Logger.LogInformation("SomeDate: {SomeDate}", SomeDate);
            SomeDate = Now();

            UpdateProgress(66);

            SimpleInt += 5;
            IgnoreData = "x";

            for (int i = 0; i < 1250; i++)
            {
                try
                {
                    // Do Something 1

                    // Do Something 2

                    // Do Something 3

                    // Do Something 4

                    fact.IncreaseEffectedRows();
                }
                catch (Exception ex)
                {
                    fact.AddAggregateException(ex);
                }
                finally
                {
                    UpdateProgress(i, 1250);

                    ////if(CheckIfStopRequest())
                    ////{
                    ////    // exist procedure
                    ////}
                }
            }

            var a1 = fact.GetData<int>("X");
            var b1 = fact.GetEffectedRows();

            var no = fact.GetData("NoExists");
            Logger.LogInformation(no);
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            services.AddTransient<WorkerChild>();
            services.AddTransient<GeneralUtil>();
        }
    }
}