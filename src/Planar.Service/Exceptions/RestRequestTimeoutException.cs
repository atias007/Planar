using System;

namespace Planar.Service.Exceptions
{
    public sealed class RestRequestTimeoutException : Exception
    {
        public RestRequestTimeoutException()
        {
        }

        public RestRequestTimeoutException(object value)
        {
            Value = value;
        }

        public object? Value { get; private set; }
    }
}