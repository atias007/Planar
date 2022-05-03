using System.Collections.Generic;

namespace Planar
{
    public class JobExecutionContext
    {
        public Dictionary<string, string> JobSettings { get; set; }

        public string FireInstanceId { get; set; }
    }
}