using System;

namespace Planar.Job.Test.Common
{
    public sealed class PlanarJobTestException : Exception
    {
        public PlanarJobTestException(string message) : base(message)
        {
        }
    }
}