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
        private readonly RabbitMqJobStartProperties _properties = new RabbitMqJobStartProperties();

        public RabbitMqJobStartPropertiesBuilder()
        {
        }

        public RabbitMqJobStartPropertiesBuilder WithApplicationHost(IHost applicationHost)
        {
            _properties.ApplicationHost = applicationHost ?? throw new ArgumentNullException(nameof(applicationHost));
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithPlanarHostName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) { throw new ArgumentNullException(nameof(hostName)); }
            _properties.PlanarHostname = hostName;
            return this;
        }

        public RabbitMqJobStartPropertiesBuilder WithExchangeName(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName)) { throw new ArgumentNullException(nameof(exchangeName)); }
            _properties.ExchangeName = exchangeName;
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

    public class JobDefinition
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

    public class RabbitMqJobStartProperties : PlanarJobStartProperties, IHostetJobProperties
    {
        private const string DefaultExchange = "Planar";

        public ConnectionFactory RabbitMQConnectionFactory { get; internal set; } = new ConnectionFactory();
        public string PlanarHostname { get; internal set; } = string.Empty;
        public string ExchangeName { get; internal set; } = DefaultExchange;
#if NETSTANDARD2_0
        public IHost ApplicationHost { get; internal set; }
#else
        public IHost? ApplicationHost { get; internal set; }
#endif
        public IEnumerable<AmqpTcpEndpoint> RabbitMqEndpoints { get; internal set; } = new List<AmqpTcpEndpoint>();
        public IEnumerable<JobDefinition> JobDefinitions { get; internal set; } = new List<JobDefinition>();
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);

        internal JobDefinition GetJobDefinition(string name)
        {
            var jobDefinition = JobDefinitions.FirstOrDefault(jd => jd.QueueName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return jobDefinition ??
                throw new InvalidOperationException($"No job definition found for job type name '{name}'. Ensure that the job type has been added using AddJobType<TJob>() or AddJobType<TJob>(string queueName) in the RabbitMqJobStartPropertiesBuilder.");
        }
    }
}