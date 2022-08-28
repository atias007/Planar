using System;

namespace Planar.Job
{
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}