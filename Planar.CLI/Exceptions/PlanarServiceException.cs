using System;

namespace Planar.CLI.Exceptions
{
    [Serializable]
    public class PlanarServiceException : Exception
    {
        public PlanarServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }
    }
}