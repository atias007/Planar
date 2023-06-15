using System;

namespace Planar.API.Common.Entities
{
    public class ConcurrentExecutionModel
    {
        public DateTime RecordDate { get; set; }

        public string Server { get; set; } = null!;

        public string InstanceId { get; set; } = null!;

        public int MaxConcurrent { get; set; }
    }
}