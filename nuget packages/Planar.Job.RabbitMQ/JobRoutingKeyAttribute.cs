using System;

namespace Planar.Job
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class JobRoutingKeyAttribute : Attribute
    {
        public string RoutingKey { get; }

        public JobRoutingKeyAttribute(string routingKey)
        {
            if (string.IsNullOrWhiteSpace(routingKey))
            {
                throw new ArgumentNullException(nameof(routingKey), "Routing key cannot be null or whitespace.");
            }

            RoutingKey = routingKey;
        }
    }
}