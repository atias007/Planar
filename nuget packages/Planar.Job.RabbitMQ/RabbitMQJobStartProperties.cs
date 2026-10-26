using Microsoft.Extensions.Hosting;
using Planar.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

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
            ValidateJobType(jobType);
            var attribute = ValidateJobQueueNameAttribute(jobType);

            if (attribute == null)
            {
                throw new InvalidOperationException($"Job type '{jobType.Name}' does not have a [JobRoutingKey] attribute");
            }
            else
            {
                _properties.AddJobDefinition(new JobDefinition(jobType, attribute.RoutingKey));
            }

            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddJob<TJob>(string routingKey) where TJob : BaseJob, new()
        {
            if (string.IsNullOrWhiteSpace(routingKey)) { throw new ArgumentNullException(nameof(routingKey)); }
            var jobType = typeof(TJob);
            ValidateJobType(jobType);
            ValidateRoutingKey(routingKey);

            _properties.AddJobDefinition(new JobDefinition(jobType, routingKey));
            return this;
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

        private void ValidateRoutingKey(string routingKey)
        {
            if (_properties.JobDefinitions.Any(d => string.Equals(d.RoutingKey, routingKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A job definition for routing key '{routingKey}' already exists. Use AddJob<TJob>(string routingKey) to specify a different routing key.");
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

    public class JobDefinition : IJobDefinition
    {
        public JobDefinition(Type jobType, string routingKey)
        {
            if (string.IsNullOrWhiteSpace(routingKey)) { throw new ArgumentException("Routing key cannot be empty or whitespace", nameof(routingKey)); }

            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            RoutingKey = routingKey;
        }

        public Type JobType { get; private set; }
        public string RoutingKey { get; private set; }
    }

    public class RabbitMqJobStartProperties : PlanarHostedJobStartProperties<JobDefinition>
    {
        private const string DefaultExchange = "Planar";

        public ConnectionFactory RabbitMQConnectionFactory { get; internal set; } = new ConnectionFactory();

        public string Exchange { get; internal set; } = DefaultExchange;

        public string QueueName { get; internal set; } = DefaultExchange;

#if NETSTANDARD2_0

        public string DeadLetterExchange { get; set; }
#else
        public string? DeadLetterExchange { get; set; }
#endif

#if NETSTANDARD2_0

        public string DeadLetterRoutingKey { get; set; }
#else
        public string? DeadLetterRoutingKey { get; set; }
#endif

        public IEnumerable<AmqpTcpEndpoint> RabbitMqEndpoints { get; internal set; } = new List<AmqpTcpEndpoint>();

        internal JobDefinition GetJobDefinition(string routingKey)
        {
            var jobDefinition = JobDefinitions.FirstOrDefault(jd => jd.RoutingKey.Equals(routingKey, StringComparison.OrdinalIgnoreCase));
            return jobDefinition ??
                throw new InvalidOperationException($"No job definition found with routing key '{routingKey}'. Ensure that the job type has been added using AddJobType<TJob>() or AddJobType<TJob>(string queueName) in the RabbitMqJobStartPropertiesBuilder.");
        }
    }
}