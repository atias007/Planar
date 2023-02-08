using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using Planar.Job;

namespace SomeJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogWarning("Now: {Now}", Now());

            var maxDiffranceHours = Configuration.GetValue("Max Diffrance Hours", 12);
            Logger.LogInformation("Value is {Value} while default value is 12", maxDiffranceHours);
            AddAggragateException(new Exception("agg ex"));

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

            await Task.Delay(2000);
            Logger.LogWarning("JobRunTime: {JobRunTime}", JobRunTime);

            UpdateProgress(66);
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}