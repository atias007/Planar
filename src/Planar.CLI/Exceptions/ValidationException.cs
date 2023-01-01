using System;

namespace Planar.CLI.Exceptions
{
    [Serializable]
    public class CliValidationException : Exception
    {
        public CliValidationException(string message) : base(message)
        {
        }
    }
}