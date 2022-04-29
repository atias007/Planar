using System;

namespace Planar.Job
{
    public class PlanarJobAggragateException : Exception
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }
    }
}