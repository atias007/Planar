using System;

namespace Planar.Client.Entities
{
    public class MonitorAlertDetails : MonitorAlertBasicDetails
    {
        public int MonitorId { get; set; }

#if NETSTANDARD2_0
        public string LogInstanceId { get; set; }
        public string Exception { get; set; }
        public string AlertPayload { get; set; }
#else

        public string? LogInstanceId { get; set; }
        public string? Exception { get; set; }
        public string? AlertPayload { get; set; }
#endif

        public int UsersCount { get; set; }
    }
}