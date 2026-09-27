using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class ApplyResponse
    {
        public int TotalUpdate { get; set; }
        public int TotalAdd { get; set; }
        public int TotalDelete { get; set; }
        public int TotalUnchanged { get; set; }
        public int TotalErrors { get; set; }
        public List<ApplyResponseItem> Items { get; set; } = new List<ApplyResponseItem>();
    }

    public class ApplyResponseItem
    {
        public string Key { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int ActionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
#if NETSTANDARD2_0
        public string Source { get; set; }
#else
        public string? Source { get; set; }
#endif
    }
}