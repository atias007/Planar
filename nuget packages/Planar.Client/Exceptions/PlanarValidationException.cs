using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarValidationException : Exception
    {
        public IPlanarValidationErrors? Errors { get; private set; }

        internal PlanarValidationException(string message) : base(message)
        {
        }

        internal PlanarValidationException(string message, IPlanarValidationErrors errors) : base(message)
        {
            Errors = errors;
        }
    }
}