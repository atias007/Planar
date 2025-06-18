using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class PausedReportJob : BaseReportJob<PausedReportJob>, IJob
{
    public static ReportNames ReportName => ReportNames.Paused;

    public PausedReportJob(IServiceScopeFactory serviceScope, ILogger<PausedReportJob> logger)
        : base(serviceScope, logger)
    {
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<PausedJobsReport>(context, ReportName);
    }
}