using System;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public sealed class RestNotFoundException : Exception
    {
        public RestNotFoundException()
        {
        }

        public RestNotFoundException(object value)
        {
            Value = value;
        }

        public object? Value { get; private set; }
    }
}