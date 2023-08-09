using System;
using System.Runtime.Serialization;

namespace Planar.Common
{
    [Serializable]
    public class ExecuteJobPropertiesBuilderException : Exception
    {
        public ExecuteJobPropertiesBuilderException(string message) : base(message)
        {
        }

        protected ExecuteJobPropertiesBuilderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}