using Planar.Common.Exceptions;
using System.Runtime.Serialization;

namespace Planar
{
    [Serializable]
    public class RestJobException : PlanarException
    {
        public RestJobException(string message) : base(message)
        {
        }

        public RestJobException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RestJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}