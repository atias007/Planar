using System;

namespace Planar.Client.Entities
{
    public class ConcurrentExecutionDetails
    {
        public DateTime RecordDate { get; set; }

        public string Server { get; set; } = null!;

        public string InstanceId { get; set; } = null!;

        public int MaxConcurrent { get; set; }
    }
}