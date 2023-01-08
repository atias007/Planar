using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestGeneralException : Exception
    {
        public RestGeneralException(string message) : base(message)
        {
        }

        public RestGeneralException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RestGeneralException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public int? StatusCode { get; set; }
    }
}