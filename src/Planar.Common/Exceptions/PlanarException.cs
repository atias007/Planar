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

        protected PlanarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}