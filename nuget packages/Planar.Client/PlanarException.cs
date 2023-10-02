using System;

namespace Planar.Client
{
    public class PlanarException : Exception
    {
        public PlanarException(string message) : base(message)
        {
        }
    }
}