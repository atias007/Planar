using System;

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

        public object Value { get; private set; }
    }
}