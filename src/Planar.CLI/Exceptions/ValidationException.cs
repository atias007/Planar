using System;

namespace Planar.CLI.Exceptions;

public sealed class CliValidationException(string message) : Exception(message)
{
}