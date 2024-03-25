using System;

namespace Planar.CLI.Exceptions
{
    public sealed class CliValidationException : Exception
    {
        public CliValidationException(string message) : base(message)
        {
        }
    }
}