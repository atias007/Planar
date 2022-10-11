using System;

namespace Planar.Job
{
    [Serializable]
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}