namespace Planner.API.Common.Entities
{
    public class GetServiceInfoResponse : BaseResponse
    {
        public bool InStandbyMode { get; set; }

        public bool IsShutdown { get; set; }

        public bool IsStarted { get; set; }

        public string SchedulerInstanceId { get; set; }

        public string SchedulerName { get; set; }

        public string Environment { get; set; }
    }
}