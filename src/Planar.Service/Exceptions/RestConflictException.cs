using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public sealed class RestConflictException : Exception
    {
        public RestConflictException()
        {
        }

        public RestConflictException(object value)
        {
            Value = value;
        }

        public object? Value { get; private set; }
    }
}