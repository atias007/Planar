using Planar.Common.Exceptions;
using System;
using System.Runtime.Serialization;

namespace Planar
{
    [Serializable]
    public class PlanarJobException : PlanarException
    {
        public PlanarJobException(string message) : base(message)
        {
        }

        protected PlanarJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}