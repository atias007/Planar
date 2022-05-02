using System.Collections.Generic;

namespace Planar
{
    public class JobExecutionContext
    {
        public Dictionary<string, string> MergeData { get; set; }

        public object State { get; set; }
    }
}