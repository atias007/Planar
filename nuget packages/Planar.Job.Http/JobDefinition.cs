using Planar.Common;

namespace Planar.Job.Http
{
    public class JobDefinition : IJobDefinition
    {
        public JobDefinition(Type jobType)
        {
            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            Route = jobType.Name ?? throw new ArgumentException("Job type must have a name", nameof(jobType));
        }

        public JobDefinition(Type jobType, string route)
        {
            if (string.IsNullOrWhiteSpace(route)) { throw new ArgumentException("Route cannot be empty or whitespace", nameof(route)); }

            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            Route = route;
        }

        public Type JobType { get; private set; }
        public string Route { get; private set; }
    }
}