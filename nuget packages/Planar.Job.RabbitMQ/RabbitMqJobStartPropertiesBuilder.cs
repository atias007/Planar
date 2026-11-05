using Microsoft.Extensions.Hosting;
using Planar.Job.Attributes;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Planar.Job.RabbitMq
{
    public class RabbitMqJobStartPropertiesBuilder : BaseHostedJobStartPropertiesBuilder
    {
        protected readonly RabbitMqJobStartProperties _properties = new RabbitMqJobStartProperties();

        public RabbitMqJobStartPropertiesBuilder()
        {
        }

        public RabbitMqJobStartPropertiesBuilder WithEncryptionKey(string encryptionKey)
        {
            WithEncryptionKey(_properties, encryptionKey);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithHost(IHost host)
        {
            WithApplicationHost(_properties, host);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarHost(
            string hostName = "localhost",
            int port = PlanarHostedJobStartProperties.DefaultPlanarPort)
        {
            WithPlanarHost(_properties, hostName, port);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarApi(
            HttpSchema schema = HttpSchema.Http,
            int port = PlanarHostedJobStartProperties.DefaultPlanarApiPort)
        {
            WithPlanarApi(_properties, schema, port);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarApiAuthentication(string username, string password)
        {
            WithPlanarApiAuthentication(_properties, username, password);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeoutSeconds(int seconds)
        {
            WithLogFlushTimeoutSeconds(_properties, seconds);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeoutMinutes(int minutes)
        {
            WithLogFlushTimeoutMinutes(_properties, minutes);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeout(TimeSpan timeout)
        {
            WithLogFlushTimeout(_properties, timeout);
            return this;
        }

#if NETSTANDARD2_0

        public RabbitMqJobStartPropertiesBuilder AddHostSingletonType<T>()
#else
        public RabbitMqJobStartPropertiesBuilder AddHostSingletonType<T>() where T : notnull
#endif
        {
            AddHostSingletonType<T>(_properties);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithExchange(string exchange)
        {
            if (string.IsNullOrWhiteSpace(exchange)) { throw new ArgumentNullException(nameof(exchange)); }
            _properties.Exchange = exchange;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithQueueName(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ArgumentNullException(nameof(queueName)); }
            _properties.QueueName = queueName;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithDeadLetterExchange(string deadLetterExchange)
        {
            if (string.IsNullOrWhiteSpace(deadLetterExchange)) { throw new ArgumentNullException(nameof(deadLetterExchange)); }
            _properties.DeadLetterExchange = deadLetterExchange;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithDeadLetterRoutingKey(string deadLetterRoutingKey)
        {
            if (string.IsNullOrWhiteSpace(deadLetterRoutingKey)) { throw new ArgumentNullException(nameof(deadLetterRoutingKey)); }
            _properties.DeadLetterRoutingKey = deadLetterRoutingKey;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddRabbitMqEndpoints(params AmqpTcpEndpoint[] endpoints)
        {
            if (endpoints == null) { throw new ArgumentNullException(nameof(endpoints)); }
            _properties.RabbitMqEndpoints = new List<AmqpTcpEndpoint>(_properties.RabbitMqEndpoints).Concat(endpoints).ToList();
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddRabbitMqEndpoints(params string[] endpoints)
        {
            if (endpoints == null) { throw new ArgumentNullException(nameof(endpoints)); }
            foreach (var endpoint in endpoints)
            {
                _properties.RabbitMqEndpoints = new List<AmqpTcpEndpoint>(_properties.RabbitMqEndpoints) { new AmqpTcpEndpoint(endpoint) };
            }
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithRabbitMqConnectionFactory(ConnectionFactory connectionFactory)
        {
            _properties.RabbitMQConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddJob<TJob>() where TJob : BaseJob, new()
        {
            var jobType = typeof(TJob);
            return AddJob(jobType);
        }

        public RabbitMqJobStartPropertiesBuilder AddJob(Type type)
        {
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

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
            var attribute = ValidateJobQueueNameAttribute(type);

            if (attribute == null)
            {
                throw new InvalidOperationException($"Job type '{type.FullName}' does not have a [JobRoutingKey] attribute");
            }
            else
            {
                _properties.AddJobDefinition(new JobDefinition(type, attribute.RoutingKey));
            }

            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddJobsFromAssembly(params Assembly[] assemblies)
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

        public RabbitMqJobStartPropertiesBuilder AddJobsFromAssembly(Type type)
        {
            var assembly = type.Assembly;
            return AddJobsFromAssembly(assembly);
        }

        public RabbitMqJobStartPropertiesBuilder AddJobsFromAssembly<T>() where T : class
        {
            var assembly = typeof(T).Assembly;
            return AddJobsFromAssembly(assembly);
        }

        public RabbitMqJobStartPropertiesBuilder ApplyManifestFromFolder(string folderPath)
        {
            ApplyManifestFromFolder(_properties, folderPath);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder ApplyManifestFromAssembly<T>()
        {
            return ApplyManifestFromAssembly(typeof(T).Assembly);
        }

        public RabbitMqJobStartPropertiesBuilder ApplyManifestFromAssembly(params System.Reflection.Assembly[] assemblies)
        {
            ApplyManifestFromAssembly(_properties, assemblies);
            return this;
        }

        public RabbitMqJobStartProperties Build()
        {
            Validate();
            return _properties;
        }

        protected void Validate()
        {
            Validate(_properties);

            if (!_properties.JobDefinitions.Any())
            {
                throw new InvalidOperationException("At least one job type must be added. Use AddJob<TJob>() or AddJob<TJob>(string queueName) to add a job type.");
            }

            if (string.IsNullOrWhiteSpace(_properties.DeadLetterExchange) && !string.IsNullOrWhiteSpace(_properties.DeadLetterRoutingKey))
            {
                throw new InvalidOperationException("Dead letter exchange must be set if dead letter routing key is specified. Use WithDeadLetterExchange(string deadLetterExchange) to set the dead letter exchange.");
            }
        }

        private void ValidateJobType(Type jobType)
        {
            if (_properties.JobDefinitions.Any(d => d.JobType == jobType))
            {
                throw new InvalidOperationException($"A job definition for job type '{jobType.Name}' already exists. Use AddJob<TJob>() to specify a different job type.");
            }
        }

#if NETSTANDARD2_0

        private static JobRoutingKeyAttribute ValidateJobQueueNameAttribute(Type jobType)
#else
        private static JobRoutingKeyAttribute? ValidateJobQueueNameAttribute(Type jobType)
#endif
        {
            var attribute = Attribute.GetCustomAttribute(jobType, typeof(JobRoutingKeyAttribute));
            if (attribute == null) { return null; }
            return (JobRoutingKeyAttribute)attribute;
        }
    }
}