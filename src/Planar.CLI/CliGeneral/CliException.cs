using RestSharp;
using System;
using System.Runtime.Serialization;

namespace Planar.CLI
{
    [Serializable]
    public class CliException : Exception
    {
        public RestResponse? RestResponse { get; private set; }

        public CliException(string message, RestResponse restResponse) : base(message)
        {
            RestResponse = restResponse;
        }

        public CliException(string message) : base(message)
        {
        }

        public CliException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        protected CliException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}