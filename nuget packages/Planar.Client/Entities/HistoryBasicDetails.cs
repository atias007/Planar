using System;

namespace Planar.Client.Entities
{
    public class HistoryBasicDetails
    {
        public long Id { get; set; }
        public string? JobId { get; set; }
        public string? JobType { get; set; }
        public string? JobName { get; set; }
        public string? JobGroup { get; set; }
        public string? TriggerId { get; set; }
        public int Status { get; set; }
        public string? StatusTitle { get; set; }
        public DateTime StartDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
    }
}