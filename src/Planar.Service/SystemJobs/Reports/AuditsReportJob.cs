using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class AuditsReportJob(IServiceScopeFactory serviceScope, ILogger<AuditsReportJob> logger)
    : BaseReportJob<AuditsReportJob>(serviceScope, logger), IJob
{
    public static ReportNames ReportName => ReportNames.Audits;

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeExecute<AuditsReport>(context, ReportName);
    }
}