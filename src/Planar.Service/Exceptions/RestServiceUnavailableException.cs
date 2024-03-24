using System;

namespace Planar.Service.Exceptions
{
    public sealed class RestServiceUnavailableException(string message) : Exception(message)
    {
    }
}