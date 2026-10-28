using Microsoft.AspNetCore.Builder;
using Planar.Common;

namespace Planar.Job.Http
{
    public class HttpJobStartProperties : PlanarHostedJobStartProperties<JobDefinition>
    {
        internal JobDefinition GetJobDefinition(string name)
        {
            var jobDefinition = JobDefinitions.FirstOrDefault(jd => jd.Route.Equals(name, StringComparison.OrdinalIgnoreCase));
            return jobDefinition ??
                throw new PlanarJobNotFoundException($"No job definition found for job type name '{name}'. Ensure that the job type has been added using AddJobType<TJob>() or AddJobType<TJob>(string resource) in the HttpJobStartPropertiesBuilder.");
        }
    }
}