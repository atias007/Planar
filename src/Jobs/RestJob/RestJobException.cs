using Planar.Common.Exceptions;

namespace Planar
{
    public sealed class RestJobException : PlanarException
    {
        public RestJobException(string message) : base(message)
        {
        }

        public RestJobException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}