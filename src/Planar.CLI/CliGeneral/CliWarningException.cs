using System;
using System.Runtime.Serialization;

namespace Planar.CLI
{
    [Serializable]
    public class CliWarningException : Exception
    {
        public CliWarningException(string message) : base(message)
        {
        }

        protected CliWarningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}