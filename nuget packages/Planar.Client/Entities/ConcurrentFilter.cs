using System;

namespace Planar.Client.Entities
{
    public class ConcurrentFilter : Paging, IDateScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Server { get; set; }
        public string? InstanceId { get; set; }
    }
}