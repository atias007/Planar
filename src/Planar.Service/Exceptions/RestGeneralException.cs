using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    public sealed class RestGeneralException : Exception
    {
        public RestGeneralException(string message) : base(message)
        {
        }

        public RestGeneralException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public int? StatusCode { get; set; }
    }
}