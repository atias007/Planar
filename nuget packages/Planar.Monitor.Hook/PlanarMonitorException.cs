using System;
using System.Runtime.Serialization;

namespace Planar.Monitor.Hook
{
    [Serializable]
    public class PlanarMonitorException : Exception
    {
        public PlanarMonitorException()
        {
        }

        public PlanarMonitorException(string message) : base(message)
        {
        }

        public PlanarMonitorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlanarMonitorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // ...
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}