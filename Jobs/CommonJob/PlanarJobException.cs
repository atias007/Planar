using System;

namespace CommonJob
{
    [Serializable]
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}