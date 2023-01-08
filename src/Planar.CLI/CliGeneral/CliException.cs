using System;

namespace Planar.CLI
{
    [Serializable]
    public class CliException : Exception
    {
        public CliException(string message) : base(message)
        {
        }
    }
}