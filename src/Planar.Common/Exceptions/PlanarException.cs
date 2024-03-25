using System;

namespace Planar.Common.Exceptions
{
    public class PlanarException : Exception
    {
        public PlanarException(string message) : base(message)
        {
        }

        public PlanarException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}