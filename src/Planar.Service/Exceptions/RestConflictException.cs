using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestConflictException : Exception
    {
        public RestConflictException()
        {
        }

        public RestConflictException(object value)
        {
            Value = value;
        }

        protected RestConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public object? Value { get; private set; }
    }
}