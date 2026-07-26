using System;

namespace Planar.Service.Exceptions;

public sealed class RestServiceUnavailableException : Exception
{
    private const string DefaultMessage = "Service is unavailable";
    private readonly object? _body;

    public RestServiceUnavailableException(string message) : base(message)
    {
    }

    public RestServiceUnavailableException(object body) : base(DefaultMessage)
    {
        _body = body;
    }

    public object? Body => _body;
}