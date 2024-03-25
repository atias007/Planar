using System;

namespace Planar.Job
{
    public sealed class PlanarJobException : Exception
    {
        public PlanarJobException()
        {
        }

        public PlanarJobException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PlanarJobException(string message) : base(message)
        {
        }
    }
}