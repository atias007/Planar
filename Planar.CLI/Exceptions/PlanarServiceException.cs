using Planar.API.Common.Entities;
using System;

namespace Planar.CLI.Exceptions
{
    public class PlanarServiceException : Exception
    {
        public PlanarServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }
    }
}