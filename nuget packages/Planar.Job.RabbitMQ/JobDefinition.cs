using Planar.Common;
using System;

namespace Planar.Job.RabbitMq
{
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
}