using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class TraceReportJob(IServiceScopeFactory serviceScope, ILogger<TraceReportJob> logger)
    : BaseReportJob<TraceReportJob>(serviceScope, logger), IJob
{
    public static ReportNames ReportName => ReportNames.Trace;

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<TraceReport>(context, ReportName);
    }
}