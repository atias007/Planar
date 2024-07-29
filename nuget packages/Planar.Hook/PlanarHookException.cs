using System;

namespace Planar.Hook;

public sealed class PlanarHookException : Exception
{
    public PlanarHookException()
    {
    }

    public PlanarHookException(string message) : base(message)
    {
    }

    public PlanarHookException(string message, Exception innerException) : base(message, innerException)
    {
    }
}