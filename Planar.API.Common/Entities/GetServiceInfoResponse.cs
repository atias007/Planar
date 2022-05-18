namespace Planar.API.Common.Entities
{
    public class GetServiceInfoResponse
    {
        public bool InStandbyMode { get; set; }

        public bool IsShutdown { get; set; }

        public bool IsStarted { get; set; }

        public string SchedulerInstanceId { get; set; }

        public string SchedulerName { get; set; }

        public string Environment { get; set; }
    }
}