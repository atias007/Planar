using System;
using System.Runtime.Serialization;

namespace Planar.CLI
{
    [Serializable]
    public class CliException : Exception
    {
        public CliException(string message) : base(message)
        {
        }

        protected CliException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}