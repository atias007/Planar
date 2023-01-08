using System;
using System.Runtime.Serialization;

namespace Planar.CLI.Exceptions
{
    [Serializable]
    public class PlanarServiceException : Exception
    {
        public PlanarServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public PlanarServiceException(string message)
            : base(message)
        {
        }

        protected PlanarServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}