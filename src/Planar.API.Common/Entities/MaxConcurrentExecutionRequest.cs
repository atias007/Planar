using System;

namespace Planar.API.Common.Entities
{
    public class MaxConcurrentExecutionRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}