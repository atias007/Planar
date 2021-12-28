using Quartz;

namespace RunPlannerJob
{
    [DisallowConcurrentExecution]
    public class PlannerJob : BasePlannerJob<PlannerJob>
    {
    }
}