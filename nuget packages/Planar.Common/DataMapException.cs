using System;

namespace Planar;

public sealed class DataMapException : Exception
{
    public DataMapException(string message) : base(message)
    {
    }
}