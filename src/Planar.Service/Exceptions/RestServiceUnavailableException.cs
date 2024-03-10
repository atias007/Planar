using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public sealed class RestServiceUnavailableException(string message) : Exception(message)
    {
    }
}