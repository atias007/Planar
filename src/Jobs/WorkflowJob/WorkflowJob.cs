using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

public abstract class WorkflowJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : BaseCommonJob<WorkflowJobProperties>(logger, dataLayer, jobMonitorUtil)
{
    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            //ValidateSqlJob();
            StartMonitorDuration(context);
            //var task = Task.Run(() => ExecuteSql(context));
            //await WaitForJobTask(context, task);
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            FinalizeJob(context);
        }
    }
}