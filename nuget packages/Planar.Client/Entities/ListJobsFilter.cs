namespace Planar.Client.Entities
{
    public enum AllJobsMembers
    {
        AllUserJobs,
        AllSystemJobs,
        All
    }

    public class ListJobsFilter : Paging
    {
        public AllJobsMembers Category { get; set; } = AllJobsMembers.AllUserJobs;

#if NETSTANDARD2_0
        public string JobType { get; set; }
        public string Group { get; set; }
        public string Filter { get; set; }
#else
        public string? JobType { get; set; }
        public string? Group { get; set; }
        public string? Filter { get; set; }
#endif

        public bool? Active { get; set; }

        public bool? Inactive { get; set; }

        internal static ListJobsFilter Empty => new ListJobsFilter();
    }
}