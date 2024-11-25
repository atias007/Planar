using System;

namespace Planar.CLI.Exceptions;

public sealed class CliValidationException : Exception
{
    public string? Suggenstion { get; }

    public CliValidationException(string message) : base(message)
    {
    }

    public CliValidationException(string message, string suggenstion) : base(message)
    {
        Suggenstion = suggenstion;
    }
}