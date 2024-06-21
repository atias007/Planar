using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class SummaryReportJob(IServiceScopeFactory serviceScope, ILogger<SummaryReportJob> logger)
    : BaseReportJob<SummaryReportJob>(serviceScope, logger), IJob
{
    public static ReportNames ReportName => ReportNames.Summary;

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<SummaryReport>(context, ReportName);
    }
}