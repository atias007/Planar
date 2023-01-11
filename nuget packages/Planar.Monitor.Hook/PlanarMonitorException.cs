using System;
using System.Runtime.Serialization;

namespace Planar.Monitor.Hook
{
    [Serializable]
    public class PlanarMonitorException : Exception
    {
        public PlanarMonitorException(string message) : base(message)
        {
        }

        public PlanarMonitorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlanarMonitorException(SerializationInfo info, StreamingContext context)
        {
            // ...
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}