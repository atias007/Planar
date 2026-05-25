using Microsoft.AspNetCore.Builder;
using Planar.Common;

namespace Planar.Job.Http
{
    public class HttpJobStartPropertiesBuilder
    {
        private readonly HttpJobStartProperties _properties = new();

        public HttpJobStartPropertiesBuilder()
        {
        }

        public HttpJobStartPropertiesBuilder WithHost(WebApplication webApplication)
        {
            _properties.Host = webApplication ?? throw new ArgumentNullException(nameof(webApplication));
            return this;
        }

        public HttpJobStartPropertiesBuilder WithPlanarHostName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) { throw new ArgumentNullException(nameof(hostName)); }
            _properties.PlanarHostname = hostName;
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeoutSeconds(int seconds)
        {
            if (seconds <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(seconds)); }
            _properties.LogFlushTimeout = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeoutMinutes(int minutes)
        {
            if (minutes <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(minutes)); }
            _properties.LogFlushTimeout = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(timeout)); }
            _properties.LogFlushTimeout = timeout;
            return this;
        }

#if NETSTANDARD2_0

        public HttpJobStartPropertiesBuilder AddHostSingletonType<T>()
#else

        public HttpJobStartPropertiesBuilder AddHostSingletonType<T>() where T : notnull
#endif
        {
            _properties.AddHostSingletonType<T>();
            return this;
        }

        public HttpJobStartPropertiesBuilder AddJob<TJob>() where TJob : BaseJob, new()
        {
            var jobType = typeof(TJob);
            ValidateJobType(jobType);
            var attribute = ValidateJobRouteAttribute(jobType);

            if (attribute == null)
            {
                _properties.JobDefinitions = [.. _properties.JobDefinitions, new JobDefinition(jobType)];
            }
            else
            {
                _properties.JobDefinitions = [.. _properties.JobDefinitions, new JobDefinition(jobType, attribute.Route)];
            }

            return this;
        }

        public HttpJobStartPropertiesBuilder AddJob<TJob>(string queueName) where TJob : BaseJob, new()
        {
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ArgumentNullException(nameof(queueName)); }
            var jobType = typeof(TJob);
            ValidateJobType(jobType);
            ValidateRoute(queueName);

            _properties.JobDefinitions = [.. _properties.JobDefinitions, new JobDefinition(jobType, queueName)];
            return this;
        }

        public HttpJobStartProperties Build()
        {
            if (!_properties.JobDefinitions.Any())
            {
                throw new InvalidOperationException("At least one job type must be added. Use AddJob<TJob>() or AddJob<TJob>(string queueName) to add a job type.");
            }

            if (string.IsNullOrWhiteSpace(_properties.PlanarHostname))
            {
                throw new InvalidOperationException("Planar hostname must be set. Use WithHostName(string hostName) to set the hostname.");
            }

            return _properties;
        }

        private void ValidateJobType(Type jobType)
        {
            if (_properties.JobDefinitions.Any(d => d.JobType == jobType))
            {
                throw new InvalidOperationException($"A job definition for job type '{jobType.Name}' already exists. Use AddJob<TJob>() to specify a different job type.");
            }
        }

        private void ValidateRoute(string route)
        {
            if (!IsValidRouteSegment(route))
            {
                throw new InvalidOperationException("Invalid route segment. Route segments must be non-empty, cannot start or end with a dot, and can only contain letters, digits, hyphens, underscores, and dots. Additionally, route segments cannot exceed 256 characters in length.");
            }

            if (_properties.JobDefinitions.Any(d => string.Equals(d.Route, route, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A job definition for route '{route}' already exists. Use AddJob<TJob>(string route) to specify a different route.");
            }
        }

        private static bool IsValidRouteSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            segment = segment.Trim();

            if (segment.Length == 0 || segment.Length > 256)
            {
                return false;
            }

            const char dot = '.';
            if (segment.StartsWith(dot) || segment.EndsWith(dot))
            {
                return false;
            }

            foreach (char c in segment)
            {
                bool isValidChar = char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.';
                if (!isValidChar)
                {
                    return false;
                }
            }

            return true;
        }

#if NETSTANDARD2_0

        private static JobRouteAttribute ValidateJobRouteAttribute(Type jobType)
#else

        private static JobRouteAttribute? ValidateJobRouteAttribute(Type jobType)
#endif
        {
            var attribute = Attribute.GetCustomAttribute(jobType, typeof(JobRouteAttribute));
            if (attribute == null) { return null; }
            return (JobRouteAttribute)attribute;
        }
    }

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