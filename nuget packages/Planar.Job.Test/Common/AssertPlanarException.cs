using System;

namespace Planar.Job.Test.Common
{
    public sealed class AssertPlanarException : Exception
    {
        public AssertPlanarException(string message) : base(message)
        {
        }
    }
}