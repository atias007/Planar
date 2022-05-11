﻿using System;
using System.Collections.Generic;

namespace Planar
{
    public class JobExecutionContext : IJobExecutionContext
    {
        public Dictionary<string, string> JobSettings { get; set; }

        public string FireInstanceId { get; set; }

        public DateTimeOffset FireTime { get; set; }

        public DateTimeOffset? NextFireTime { get; set; }

        public DateTimeOffset? ScheduledFireTime { get; set; }

        public DateTimeOffset? PreviousFireTime { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }
    }
}