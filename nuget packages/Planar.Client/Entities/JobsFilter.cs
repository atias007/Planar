namespace Planar.Client.Entities
{
    public enum AllJobsMembers
    {
        AllUserJobs,
        AllSystemJobs,
        All
    }

    public class JobsFilter : PagingRequest
    {
        public AllJobsMembers Category { get; set; } = AllJobsMembers.AllUserJobs;

        public string? JobType { get; set; }

        public string? Group { get; set; }

        public bool? Active { get; set; }

        public bool? Inactive { get; set; }

        public string? Filter { get; set; }

        internal static JobsFilter Empty => new JobsFilter();
    }
}