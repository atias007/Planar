using System;

namespace RunPlanarJob
{
    public class PlanarJobException : Exception
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}