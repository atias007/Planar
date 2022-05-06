using System;

namespace Planar.Service.Exceptions
{
    public class PlanarValidationException : Exception
    {
        public PlanarValidationException(string message) : base(message)
        {
        }

        public PlanarValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}