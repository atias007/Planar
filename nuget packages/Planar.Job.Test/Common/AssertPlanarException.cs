using System;

namespace Planar.Job.Test.Common
{
    public class AssertPlanarException : Exception
    {
        public AssertPlanarException(string message) : base(message)
        {
        }
    }
}