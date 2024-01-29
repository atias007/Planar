using System;

namespace CommonJob
{
    public sealed class JobMonitorException : Exception
    {
        public JobMonitorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}