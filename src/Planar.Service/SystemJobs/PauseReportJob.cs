using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Reports;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class PauseReportJob : BaseReportJob<PauseReportJob>, IJob
{
    public static ReportNames ReportName => ReportNames.Pause;

    public PauseReportJob(IServiceScopeFactory serviceScope, ILogger<PauseReportJob> logger)
        : base(serviceScope, logger)
    {
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await Execute<PauseReport>(context, ReportName);
    }
}