using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarException : BaseException
    {
        internal PlanarException(RestResponse response) : base(response)
        {
        }

        internal PlanarException(string message) : base(message)
        {
        }

        internal PlanarException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}