using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.General;

public interface IClusterUtil
{
    Task SequenceSignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, int index, int eventId);
}