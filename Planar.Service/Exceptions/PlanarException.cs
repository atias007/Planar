using System;

namespace Planar.Service.Exceptions
{
    public class PlanarException : Exception
    {
        public PlanarException(string message) : base(message)
        {
        }
    }
}