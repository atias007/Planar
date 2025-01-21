using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.General;

public interface IClusterUtil
{
    Task WorkflowSignalEvent(JobKey stepJobKey, string fireInstanceId, string workflowFireInstanceId, int eventId);
}