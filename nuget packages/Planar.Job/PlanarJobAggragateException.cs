using System;
using System.Runtime.Serialization;

namespace Planar
{
    [Serializable]
    public class PlanarJobAggragateException : Exception
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }

        public PlanarJobAggragateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}