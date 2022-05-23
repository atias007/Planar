using System;

namespace Planar
{
    public class PlanarJobAggragateException : Exception
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }
    }
}