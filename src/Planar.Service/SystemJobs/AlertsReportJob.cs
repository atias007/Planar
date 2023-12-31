using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class AlertsReportJob : BaseReportJob<AlertsReportJob>, IJob
{
    public static ReportNames ReportName => ReportNames.Alerts;

    public AlertsReportJob(IServiceScopeFactory serviceScope, ILogger<AlertsReportJob> logger)
        : base(serviceScope, logger)
    {
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await Execute<AlertsReport>(context, ReportName);
    }
}