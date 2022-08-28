using System;

namespace Planar.MonitorHook
{
    public class PlanarMonitorException : Exception
    {
        public PlanarMonitorException(string message) : base(message)
        {
        }

        public PlanarMonitorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}