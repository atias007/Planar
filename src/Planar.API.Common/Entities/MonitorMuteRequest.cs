using System;

namespace Planar.API.Common.Entities
{
    public class MonitorMuteRequest : MonitorUnmuteRequest
    {
        public DateTime DueDate { get; set; }
    }
}