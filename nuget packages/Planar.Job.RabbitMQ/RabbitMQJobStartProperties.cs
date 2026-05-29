using Microsoft.Extensions.Hosting;
using Planar.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Job.RabbitMq
{
    public class RabbitMqJobStartPropertiesBuilder
    {
        protected readonly RabbitMqJobStartProperties _properties = new RabbitMqJobStartProperties();

        public RabbitMqJobStartPropertiesBuilder()
        {
        }

        public RabbitMqJobStartPropertiesBuilder WithHost(IHost host)
        {
            _properties.Host = host ?? throw new ArgumentNullException(nameof(host));
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarHostName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) { throw new ArgumentNullException(nameof(hostName)); }
            _properties.PlanarHostname = hostName;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarPort(int port)
        {
            if (port > 0 || port < 65_535) { throw new ArgumentException($"Port {port} is invalid"); }
            _properties.PlanarPort = port;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeoutSeconds(int seconds)
        {
            if (seconds <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(seconds)); }
            _properties.LogFlushTimeout = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeoutMinutes(int minutes)
        {
            if (minutes <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(minutes)); }
            _properties.LogFlushTimeout = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithLogFlushTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(timeout)); }
            _properties.LogFlushTimeout = timeout;
            return this;
        }

#if NETSTANDARD2_0

        public RabbitMqJobStartPropertiesBuilder AddHostSingletonType<T>()
#else
        public RabbitMqJobStartPropertiesBuilder AddHostSingletonType<T>() where T : notnull
#endif
        {
            _properties.AddHostSingletonType<T>();
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithExchange(string exchange)
        {
            if (string.IsNullOrWhiteSpace(exchange)) { throw new ArgumentNullException(nameof(exchange)); }
            _properties.Exchange = exchange;
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
                _properties.JobDefinitions = new List<JobDefinition>(_properties.JobDefinitions) { new JobDefinition(jobType) };
            }
            else
            {
                _properties.JobDefinitions = new List<JobDefinition>(_properties.JobDefinitions) { new JobDefinition(jobType, attribute.QueueName) };
            }

            return this;
        }

        public RabbitMqJobStartPropertiesBuilder AddJob<TJob>(string queueName) where TJob : BaseJob, new()
        {
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ArgumentNullException(nameof(queueName)); }
            var jobType = typeof(TJob);
            ValidateJobType(jobType);
            ValidateQueueName(queueName);

            _properties.JobDefinitions = new List<JobDefinition>(_properties.JobDefinitions) { new JobDefinition(jobType, queueName) };
            return this;
        }

        public RabbitMqJobStartProperties Build()
        {
            if (!_properties.JobDefinitions.Any())
            {
                throw new InvalidOperationException("At least one job type must be added. Use AddJob<TJob>() or AddJob<TJob>(string queueName) to add a job type.");
            }

            if (string.IsNullOrWhiteSpace(_properties.PlanarHostname))
            {
                throw new InvalidOperationException("Planar hostname must be set. Use WithHostName(string hostName) to set the hostname.");
            }

            if (string.IsNullOrWhiteSpace(_properties.DeadLetterExchange) && !string.IsNullOrWhiteSpace(_properties.DeadLetterRoutingKey))
            {
                throw new InvalidOperationException("Dead letter exchange must be set if dead letter routing key is specified. Use WithDeadLetterExchange(string deadLetterExchange) to set the dead letter exchange.");
            }

            if (_properties.HostSingletonTypes.Any() && _properties.Host == null)
            {
                throw new InvalidOperationException("Host must be set if host singleton types are specified. Use WithHost(IHost host) to set the host.");
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

        private void ValidateQueueName(string queueName)
        {
            if (_properties.JobDefinitions.Any(d => string.Equals(d.QueueName, queueName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A job definition for queue name '{queueName}' already exists. Use AddJob<TJob>(string queueName) to specify a different queue name.");
            }
        }

#if NETSTANDARD2_0

        private JobQueueNameAttribute ValidateJobQueueNameAttribute(Type jobType)
#else
        private JobQueueNameAttribute? ValidateJobQueueNameAttribute(Type jobType)
#endif
        {
            var attribute = Attribute.GetCustomAttribute(jobType, typeof(JobQueueNameAttribute));
            if (attribute == null) { return null; }
            return (JobQueueNameAttribute)attribute;
        }
    }

    public class JobDefinition : IJobDefinition
    {
        public JobDefinition(Type jobType)
        {
            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            QueueName = jobType.Name ?? throw new ArgumentException("Job type must have a name", nameof(jobType));
        }

        public JobDefinition(Type jobType, string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { throw new ArgumentException("Queue name cannot be empty or whitespace", nameof(queueName)); }

            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            QueueName = queueName;
        }

        public Type JobType { get; private set; }
        public string QueueName { get; private set; }
    }

    public class RabbitMqJobStartProperties : PlanarHostedJobStartProperties<JobDefinition>
    {
        private const string DefaultExchange = "Planar";

        public ConnectionFactory RabbitMQConnectionFactory { get; internal set; } = new ConnectionFactory();

        public string Exchange { get; internal set; } = DefaultExchange;
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

        internal JobDefinition GetJobDefinition(string name)
        {
            var jobDefinition = JobDefinitions.FirstOrDefault(jd => jd.QueueName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return jobDefinition ??
                throw new InvalidOperationException($"No job definition found for job type name '{name}'. Ensure that the job type has been added using AddJobType<TJob>() or AddJobType<TJob>(string queueName) in the RabbitMqJobStartPropertiesBuilder.");
        }
    }
}