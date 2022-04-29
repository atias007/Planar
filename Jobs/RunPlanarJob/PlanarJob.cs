using Quartz;

namespace RunPlanarJob
{
    [DisallowConcurrentExecution]
    public class PlanarJob : BasePlanarJob<PlanarJob>
    {
    }
}