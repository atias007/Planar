using System;

namespace Planar.Job
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class JobQueueNameAttribute : Attribute
    {
        public string QueueName { get; }

        public JobQueueNameAttribute(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName), "Queue name cannot be null or whitespace.");
            }

            QueueName = queueName;
        }
    }
}