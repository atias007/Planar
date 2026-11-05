using Microsoft.AspNetCore.Builder;
using System.Reflection;

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

        public HttpJobStartPropertiesBuilder WithPlanarApiAuthentication(string username, string password)
        {
            WithPlanarApiAuthentication(_properties, username, password);
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
            return AddJob(jobType);
        }

        public HttpJobStartPropertiesBuilder AddJob(Type type)
        {
            // validate that type is a subclass of BaseJob & validate that ih has parameterless constructor
            if (!typeof(BaseJob).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type '{type.FullName}' is not a subclass of {nameof(BaseJob)}");
            }

            if (type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new InvalidOperationException($"Type '{type.FullName}' must be concrete and closed with a public parameterless constructor");
            }

            ValidateJobType(type);
            var attribute = ValidateJobRouteAttribute(type);

            if (attribute == null)
            {
                throw new InvalidOperationException($"Job type '{type.FullName}' does not have a [JobRoute] attribute");
            }
            else
            {
                _properties.AddJobDefinition(new JobDefinition(type, attribute.Route));
            }

            return this;
        }

        public HttpJobStartPropertiesBuilder AddJobsFromAssembly(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                // get all types that are subclass of BaseJob in assembly
                var jobTypes = assembly.GetTypes().Where(t =>
                    typeof(BaseJob).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.ContainsGenericParameters &&
                    t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var item in jobTypes)
                {
                    AddJob(item);
                }
            }

            return this;
        }

        public HttpJobStartPropertiesBuilder AddJobsFromAssembly(Type type)
        {
            var assembly = type.Assembly;
            return AddJobsFromAssembly(assembly);
        }

        public HttpJobStartPropertiesBuilder AddJobsFromAssembly<T>() where T : class
        {
            var assembly = typeof(T).Assembly;
            return AddJobsFromAssembly(assembly);
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
}