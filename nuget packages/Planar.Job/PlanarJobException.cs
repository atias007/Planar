using System;
using System.Runtime.Serialization;

namespace Planar.Job
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    [Serializable]
    public class PlanarJobException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        public PlanarJobException()
        {
        }

        public PlanarJobException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PlanarJobException(string message) : base(message)
        {
        }

        public PlanarJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}