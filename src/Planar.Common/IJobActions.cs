using Planar.API.Common.Entities;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Common;

public interface IJobActions
{
    Task<JobKey> InternalJobPrepareQueueInvoke(QueueInvokeJobRequest request);

    Task<PlanarIdResponse> InternalJobQueueInvoke(QueueInvokeJobRequest request, JobKey jobKey);
}