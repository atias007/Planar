using System;

namespace Planar.CLI
{
    internal class CliException : Exception
    {
        public CliException(string message) : base(message)
        {
        }
    }
}