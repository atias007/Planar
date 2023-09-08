using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestForbiddenException : Exception
    {
        public RestForbiddenException()
        {
        }

        protected RestForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}