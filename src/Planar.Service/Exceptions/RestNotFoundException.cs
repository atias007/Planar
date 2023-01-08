using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestNotFoundException : Exception
    {
        public RestNotFoundException()
        {
        }

        public RestNotFoundException(object value)
        {
            Value = value;
        }

        protected RestNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public object Value { get; private set; }
    }
}