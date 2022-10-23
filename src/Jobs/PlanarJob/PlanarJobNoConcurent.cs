using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurent : PlanarJob
    {
    }
}