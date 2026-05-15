using Microsoft.AspNetCore.Builder;
using Planar.Common;

namespace Planar.Job.Http
{
    public class JobDefinition
    {
        public JobDefinition(Type jobType)
        {
            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            Route = jobType.Name ?? throw new ArgumentException("Job type must have a name", nameof(jobType));
        }

        public JobDefinition(Type jobType, string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) { throw new ArgumentException("Resource cannot be empty or whitespace", nameof(resource)); }

            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            Route = resource;
        }

        public Type JobType { get; private set; }
        public string Route { get; private set; }
    }

    public class HttpJobStartProperties(WebApplication webApplication) : PlanarJobStartProperties, IHostetJobProperties
    {
        public WebApplication WebApplication => webApplication;
        public string PlanarHostname { get; internal set; } = string.Empty;
        public IEnumerable<JobDefinition> JobDefinitions { get; internal set; } = new List<JobDefinition>();
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);

        internal JobDefinition GetJobDefinition(string name)
        {
            var jobDefinition = JobDefinitions.FirstOrDefault(jd => jd.Route.Equals(name, StringComparison.OrdinalIgnoreCase));
            return jobDefinition ??
                throw new InvalidOperationException($"No job definition found for job type name '{name}'. Ensure that the job type has been added using AddJobType<TJob>() or AddJobType<TJob>(string resource) in the HttpJobStartPropertiesBuilder.");
        }
    }
}