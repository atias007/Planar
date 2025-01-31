using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarValidationException : Exception
    {
#if NETSTANDARD2_0
        public IPlanarValidationErrors Errors { get; private set; }

#else
        public IPlanarValidationErrors? Errors { get; private set; }

#endif

        internal PlanarValidationException(string message) : base(message)
        {
        }

        internal PlanarValidationException(string message, IPlanarValidationErrors errors) : base(message)
        {
            Errors = errors;
        }
    }
}