using Planar.Common.Exceptions;
using System;

namespace Planar;

public sealed class PlanarJobException : PlanarException
{
    public PlanarJobException(string message)
        : base(message)
    {
    }

    public PlanarJobException(string message, Exception innerException)
        : base(message, innerException)

    {
    }
}