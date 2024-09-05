using System;

namespace Planar.Client.Entities
{
    public class JobCircuitBreaker
    {
        public int FailureThreshold { get; set; }
        public int SuccessThreshold { get; set; }
        public TimeSpan? PauseSpan { get; set; }
        public int FailCounter { get; set; }
        public int SuccessCounter { get; set; }
        public DateTime? WillBeResetAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool Activated => ActivatedAt.HasValue;
    }
}