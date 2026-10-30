using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Job.RabbitMq
{
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