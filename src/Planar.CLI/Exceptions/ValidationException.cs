using System;
using System.Runtime.Serialization;

namespace Planar.CLI.Exceptions
{
    [Serializable]
    public class CliValidationException : Exception
    {
        public CliValidationException(string message) : base(message)
        {
        }

        protected CliValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}