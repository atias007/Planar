using System;

namespace Planar.Job
{
    public sealed class PlanarJobException : Exception
    {
        public PlanarJobException(string message, Exception innerException) : base(message, innerException)
        {
            HideStackTraceFromPlanar = false;
        }

        public PlanarJobException(string message) : base(message)
        {
            HideStackTraceFromPlanar = true;
        }

        public bool HideStackTraceFromPlanar { get; }
    }

    public sealed class PlanarJobConflictException : Exception
    {
        public PlanarJobConflictException(string message) : base(message)
        {
            HideStackTraceFromPlanar = true;
        }

        public bool HideStackTraceFromPlanar { get; }
    }

    public sealed class PlanarJobNotFoundException : Exception
    {
        public PlanarJobNotFoundException(string message) : base(message)
        {
            HideStackTraceFromPlanar = true;
        }

        public bool HideStackTraceFromPlanar { get; }
    }

    public sealed class PlanarJobBadRequestException : Exception
    {
        public PlanarJobBadRequestException(string message) : base(message)
        {
            HideStackTraceFromPlanar = true;
        }

        public bool HideStackTraceFromPlanar { get; }
    }
}