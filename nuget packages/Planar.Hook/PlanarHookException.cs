using System;
using System.Runtime.Serialization;

namespace Planar.Hook
{
    [Serializable]
    public class PlanarHookException : Exception
    {
        public PlanarHookException()
        {
        }

        public PlanarHookException(string message) : base(message)
        {
        }

        public PlanarHookException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlanarHookException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // ...
        }
    }
}