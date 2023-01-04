using System;

namespace Planar.Service.Monitor
{
    public class PlanarMonitorException : Exception
    {
        public PlanarMonitorException(string message) : base(message)
        {
        }
    }
}