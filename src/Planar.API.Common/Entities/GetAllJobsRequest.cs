namespace Planar.API.Common.Entities
{
    public enum AllJobsMembers
    {
        AllUserJobs,
        AllSystemJobs,
        All
    }

    public class GetAllJobsRequest : PagingRequest
    {
        public AllJobsMembers JobCategory { get; set; }

        public string? JobType { get; set; }

        public string? Group { get; set; }

        public string? Filter { get; set; }

        public bool? Active { get; set; }
    }
}