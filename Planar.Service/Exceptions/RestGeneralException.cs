using System;

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

        public int? StatusCode { get; set; }
    }
}