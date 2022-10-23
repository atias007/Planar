using System;

namespace Planar.CLI
{
    [Serializable]
    internal class CliException : Exception
    {
        public CliException(string message) : base(message)
        {
        }
    }
}