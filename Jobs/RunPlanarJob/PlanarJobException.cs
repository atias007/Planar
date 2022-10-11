using System;

namespace RunPlanarJob
{
    [Serializable]
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}