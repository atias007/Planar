﻿using System;
using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class TriggerDetails : PausedTrigger
    {
#if NETSTANDARD2_0
        public string CalendarName { get; set; }
        public string MisfireBehaviour { get; set; }
        public string State { get; set; }
        public Dictionary<string, string> DataMap { get; set; } = new Dictionary<string, string>();
#else
        public string? CalendarName { get; set; }
        public string? MisfireBehaviour { get; set; }
        public string? State { get; set; }
        public Dictionary<string, string?> DataMap { get; set; } = new Dictionary<string, string?>();
#endif

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? RetrySpan { get; set; }

        public int? MaxRetries { get; set; }

        public int Priority { get; set; }

        public DateTime? NextFireTime { get; set; }

        public DateTime? PreviousFireTime { get; set; }

        public bool MayFireAgain { get; set; }

        public DateTime? FinalFire { get; set; }

        public bool Active { get; set; }
    }
}