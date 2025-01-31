using System;

namespace Planar.Client.Entities
{
    public class ConcurrentExecutionDetails
    {
#if NETSTANDARD2_0
        public string Server { get; set; }
        public string InstanceId { get; set; }

#else
        public string Server { get; set; } = null!;
        public string InstanceId { get; set; } = null!;

#endif
        public DateTime RecordDate { get; set; }

        public int MaxConcurrent { get; set; }
    }
}