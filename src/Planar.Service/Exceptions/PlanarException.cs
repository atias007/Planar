using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class PlanarException : Exception
    {
        public PlanarException(string message) : base(message)
        {
        }

        protected PlanarException(SerializationInfo info, StreamingContext context)
        {
        }
    }
}