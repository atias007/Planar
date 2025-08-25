using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public class InvokeJobOptions
    {
        public DateTime? NowOverrideValue { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? RetrySpan { get; set; }

        public int? MaxRetries { get; set; }

#if NETSTANDARD2_0
        public Dictionary<string, string> Data { get; set; }
#else
        public Dictionary<string, string?>? Data { get; set; }
#endif
    }
}