namespace Planar.API.Common.Entities
{
    public enum AllJobsMembers
    {
        AllUserJobs,
        AllSystemJobs,
        All
    }

    public class GetAllJobsRequest
    {
        public AllJobsMembers Filter { get; set; }
    }
}