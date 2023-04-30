using System;
using System.Runtime.Serialization;

namespace Planar.Job.Test.Common
{
    [Serializable]
    public class PlanarJobTestException : Exception
    {
        public PlanarJobTestException(string message) : base(message)
        {
        }

        protected PlanarJobTestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}