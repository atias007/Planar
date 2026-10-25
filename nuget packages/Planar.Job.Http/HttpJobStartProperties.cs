using Microsoft.AspNetCore.Builder;
using Planar.Common;

namespace Planar.Job.Http
{
    public class HttpJobStartPropertiesBuilder : BaseHostedJobStartPropertiesBuilder
    {
        private readonly HttpJobStartProperties _properties = new();

        public HttpJobStartPropertiesBuilder()
        {
        }

        public HttpJobStartPropertiesBuilder WithEncryptionKey(string encryptionKey)
        {
            WithEncryptionKey(_properties, encryptionKey);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithApplicationHost(WebApplication webApplication)
        {
            WithApplicationHost(_properties, webApplication);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithPlanarHost(
            string hostName = "localhost",
            int port = PlanarHostedJobStartProperties.DefaultPlanarPort)
        {
            WithPlanarHost(_properties, hostName, port);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithPlanarApi(
            HttpSchema schema = HttpSchema.Http,
            int port = PlanarHostedJobStartProperties.DefaultPlanarApiPort)
        {
            WithPlanarApi(_properties, schema, port);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeoutSeconds(int seconds)
        {
            WithLogFlushTimeoutSeconds(_properties, seconds);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeoutMinutes(int minutes)
        {
            WithLogFlushTimeoutMinutes(_properties, minutes);
            return this;
        }

        public HttpJobStartPropertiesBuilder WithLogFlushTimeout(TimeSpan timeout)
        {
            WithLogFlushTimeout(_properties, timeout);
            return this;
        }

#if NETSTANDARD2_0

        public HttpJobStartPropertiesBuilder AddHostSingletonType<T>()
#else

        public HttpJobStartPropertiesBuilder AddHostSingletonType<T>() where T : notnull
#endif
        {
            AddHostSingletonType<T>(_properties);
            return this;
        }

        public HttpJobStartPropertiesBuilder ApplyManifestFromFolder(string folderPath)
        {
            ApplyManifestFromFolder(_properties, folderPath);
            return this;
        }

        public HttpJobStartPropertiesBuilder ApplyManifestFromAssembly<T>()
        {
            return ApplyManifestFromAssembly(typeof(T).Assembly);
        }

        public HttpJobStartPropertiesBuilder ApplyManifestFromAssembly(params System.Reflection.Assembly[] assemblies)
        {
            ApplyManifestFromAssembly(_properties, assemblies);
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

        public HttpJobStartPropertiesBuilder AddJob<TJob>(string route) where TJob : BaseJob, new()
        {
            if (string.IsNullOrWhiteSpace(route)) { throw new ArgumentNullException(nameof(route)); }
            var jobType = typeof(TJob);
            ValidateJobType(jobType);
            ValidateRoute(route);

            _properties.JobDefinitions = [.. _properties.JobDefinitions, new JobDefinition(jobType, route)];
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