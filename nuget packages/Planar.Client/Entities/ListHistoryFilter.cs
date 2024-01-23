using System;

namespace Planar.Client.Entities
{
    public class ListHistoryFilter : Paging, IDateScope
    {
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool Ascending { get; set; }

        public bool? Outlier { get; set; }

        public HistoryStatusMembers? Status { get; set; }
    }
}