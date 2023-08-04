using System;
using System.Runtime.Serialization;

namespace Planar.Common.Exceptions
{
    [Serializable]
    public class PlanarException : Exception
    {
        public PlanarException(string message) : base(message)
        {
        }

        public PlanarException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlanarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}