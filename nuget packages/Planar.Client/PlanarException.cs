using Planar.Client.Entities;
using System;

namespace Planar.Client
{
    public class PlanarException : Exception
    {
        public PlanarValidationErrors? Errors { get; private set; }

        internal PlanarException(string message) : base(message)
        {
        }

        internal PlanarException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal PlanarException(string message, PlanarValidationErrors errors) : base(message)
        {
            Errors = errors;
        }
    }
}