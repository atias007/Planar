using System;
using System.Runtime.Serialization;

namespace Planar.Common
{
    [Serializable]
    public class ExecuteJobBuilderException : Exception
    {
        public ExecuteJobBuilderException(string message) : base(message)
        {
        }

        protected ExecuteJobBuilderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}