using System;

namespace Planar.API.Common.Entities
{
    public class MaxConcurrentExecutionRequest : IDateScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}