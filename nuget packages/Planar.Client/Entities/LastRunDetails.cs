using System;

namespace Planar.Client.Entities
{
    public class LastRunDetails
    {
        public long Id { get; set; }

#if NETSTANDARD2_0
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string JobType { get; set; }
        public string TriggerId { get; set; }
        public string StatusTitle { get; set; }

#else
        public string JobId { get; set; } = null!;
        public string JobName { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string JobType { get; set; } = null!;
        public string TriggerId { get; set; } = null!;
        public string? StatusTitle { get; set; }
#endif
        public int Status { get; set; }

        public DateTime StartDate { get; set; }

        public int? Duration { get; set; }

        public int? EffectedRows { get; set; }

        public bool HasWarnings { get; set; }
    }
}