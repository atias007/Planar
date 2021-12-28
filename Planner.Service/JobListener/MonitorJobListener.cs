using Planner.Service.Monitor;
using Quartz;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.Service.JobListener
{
    public class MonitorJobListener : IJobListener
    {
        public string Name => nameof(MonitorJobListener);

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var task = MonitorUtil.Scan(MonitorEvents.ExecutionVetoed, context, null, cancellationToken);
            return task;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var task = MonitorUtil.Scan(MonitorEvents.ExecutionStart, context, null, cancellationToken);
            return task;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            var task1 = MonitorUtil.Scan(MonitorEvents.ExecutionEnd, context, jobException, cancellationToken);

            var @event =
                jobException == null ?
                MonitorEvents.ExecutionSuccess :
                MonitorEvents.ExecutionFail;

            var task2 = MonitorUtil.Scan(@event, context, jobException, cancellationToken);

            return Task.WhenAll(task1, task2);
        }
    }
}