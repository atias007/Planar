using System;
using System.Runtime.Serialization;

namespace Planar.Job.Test.Common
{
    [Serializable]
    public class AssertPlanarException : Exception
    {
        public AssertPlanarException(string message) : base(message)
        {
        }

        protected AssertPlanarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}