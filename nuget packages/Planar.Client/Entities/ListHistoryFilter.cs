using System;

namespace Planar.Client.Entities
{
    public class ListHistoryFilter : Paging, IDateScope
    {
#if NETSTANDARD2_0
        public string JobId { get; set; }

        public string JobGroup { get; set; }

        public string JobType { get; set; }
#else
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }
#endif

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool Ascending { get; set; }

        public bool? Outlier { get; set; }

        public bool? HasWarnings { get; set; }

        public HistoryStatusMembers? Status { get; set; }
    }
}