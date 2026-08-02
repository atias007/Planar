using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace RabbitMQJob;

[JobQueueName("JobA")]
internal class JobA : BaseJob
{
    // DEMO
    // -----
    /*
        {
          "JobSettings": {},
          "MergedJobDataMap": {
            "__Job_Id": "vxm2sq2pnpr",
            "__Trigger_Id": "p3p252nv5i1"
          },
          "FireInstanceId": "Default639129718952775258",
          "FireTime": "2026-04-28T11:18:15.5727125+00:00",
          "NextFireTime": "2026-04-28T11:48:15.2129826+00:00",
          "ScheduledFireTime": "2026-04-27T14:48:07.4215914+00:00",
          "PreviousFireTime": "2026-04-27T14:18:07.4215914+00:00",
          "Recovering": false,
          "RefireCount": 0,
          "JobPort": 206,
          "JobFailOverPort": 2306,
          "JobDetails": {
            "Key": {
              "Name": "HelloWorld2",
              "Group": "Demo"
            },
            "Id": "vxm2sq2pnpr",
            "Description": "Demo Hello World Job 2+2",
            "JobDataMap": {
              "__Job_Id": "vxm2sq2pnpr"
            },
            "Durable": true,
            "PersistJobDataAfterExecution": true,
            "ConcurrentExecutionDisallowed": true,
            "RequestsRecovery": true
          },
          "TriggerDetails": {
            "Key": {
              "Name": "default_trigger1",
              "Group": "vxm2sq2pnpr"
            },
            "Description": null,
            "Id": "p3p252nv5i1",
            "CalendarName": null,
            "TriggerDataMap": {
              "__Trigger_Id": "p3p252nv5i1"
            },
            "FinalFireTime": null,
            "EndTime": null,
            "StartTime": "2026-04-28T11:18:15.2129826+00:00",
            "Priority": 5,
            "HasMillisecondPrecision": true,
            "HasRetry": false,
            "IsRetryTrigger": false,
            "IsLastRetry": null,
            "RetryNumber": null,
            "MaxRetries": null,
            "RetrySpan": null,
            "Timeout": "02:00:00"
          },
          "Environment": "Development"
        }
     */

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        var s = ServiceProvider.GetRequiredService<DemoSignleton>();
        Logger.LogWarning("{CreateDate}", s.CreateDate);
        Logger.LogWarning("{HashCode}", s.GetHashCode());
        Logger.LogWarning("------------------------------");
        var dal = ServiceProvider.GetRequiredService<DataLayer>();
        var currencies = dal.GetCurrency();
        Logger.LogDebug("Currency count: {Count}", currencies.Count());
        var total = currencies.Count();
        var current = 0;
        foreach (var item in currencies)
        {
            Logger.LogInformation("{Name}: {Rate:N4}", item.Name, item.Rate);
            await IncreaseEffectedRowsAsync();
            current++;
            await base.UpdateProgressAsync(current, total);
            await Task.Delay(3000);
        }
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddScoped<DataLayer>();
    }
}