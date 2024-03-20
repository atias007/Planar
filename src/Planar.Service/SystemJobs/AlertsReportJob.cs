using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class AlertsReportJob(IServiceScopeFactory serviceScope, ILogger<AlertsReportJob> logger)
    : BaseReportJob<AlertsReportJob>(serviceScope, logger), IJob
{
    public static ReportNames ReportName => ReportNames.Alerts;

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<AlertsReport>(context, ReportName);
    }
}