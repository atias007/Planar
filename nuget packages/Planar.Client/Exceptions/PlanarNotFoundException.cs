using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarNotFoundException : Exception
    {
        internal PlanarNotFoundException()
        {
        }

        internal PlanarNotFoundException(string message) : base(message)
        {
        }
    }
}