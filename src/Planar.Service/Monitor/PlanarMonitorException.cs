using System;

namespace Planar.Service.Monitor
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    public class PlanarMonitorException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        public PlanarMonitorException(string message) : base(message)
        {
        }
    }
}