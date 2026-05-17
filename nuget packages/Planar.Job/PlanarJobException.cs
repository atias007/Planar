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

    public sealed class PlanarJobConflictException : Exception
    {
        public PlanarJobConflictException(string message) : base(message)
        {
        }
    }

    public sealed class PlanarJobNotFoundException : Exception
    {
        public PlanarJobNotFoundException(string message) : base(message)
        {
        }
    }

    public sealed class PlanarJobBadRequestException : Exception
    {
        public PlanarJobBadRequestException(string message) : base(message)
        {
        }
    }
}