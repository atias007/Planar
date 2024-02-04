using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class SummaryReportJob : BaseReportJob<SummaryReportJob>, IJob
{
    public static ReportNames ReportName => ReportNames.Summary;

    public SummaryReportJob(IServiceScopeFactory serviceScope, ILogger<SummaryReportJob> logger)
        : base(serviceScope, logger)
    {
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<SummaryReport>(context, ReportName);
    }
}