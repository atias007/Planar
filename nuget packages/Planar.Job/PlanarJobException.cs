using System;
using System.Runtime.Serialization;

namespace Planar.Job
{
    [Serializable]
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }

        public PlanarJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}