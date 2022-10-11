using System;

namespace Planar
{
    [Serializable]
    public class PlanarJobAggragateException : Exception
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }
    }
}