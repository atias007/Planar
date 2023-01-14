using System;
using System.Runtime.Serialization;

namespace CommonJob
{
    [Serializable]
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }

        protected PlanarJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}