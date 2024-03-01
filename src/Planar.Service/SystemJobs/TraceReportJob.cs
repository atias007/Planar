using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class TraceReportJob : BaseReportJob<TraceReportJob>, IJob
{
    public static ReportNames ReportName => ReportNames.Alerts;

    public TraceReportJob(IServiceScopeFactory serviceScope, ILogger<TraceReportJob> logger)
        : base(serviceScope, logger)
    {
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<TraceReport>(context, ReportName);
    }
}