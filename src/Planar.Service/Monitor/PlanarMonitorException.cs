using System;
using System.Runtime.Serialization;

namespace Planar.Service.Monitor
{
    public class PlanarMonitorException : Exception
    {
        public PlanarMonitorException(string message) : base(message)
        {
        }

        protected PlanarMonitorException(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            // ...
        }
    }
}