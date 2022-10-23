using Quartz;

namespace RunPlanarJob
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurent : PlanarJob
    {
    }
}