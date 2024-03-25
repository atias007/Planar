using System;
using System.Runtime.Serialization;

namespace Planar.CLI.Exceptions
{
    public sealed class PlanarServiceException : Exception
    {
        public PlanarServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public PlanarServiceException(string message)
            : base(message)
        {
        }
    }
}