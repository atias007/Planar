using System;

namespace Planar.CLI
{
    public sealed class CliWarningException(string message) : Exception(message)
    {
    }
}