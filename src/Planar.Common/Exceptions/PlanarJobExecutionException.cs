using System;
using System.Runtime.Serialization;

namespace Planar.Common.Exceptions
{
    [Serializable]
    public class PlanarJobExecutionException : Exception
    {
        public PlanarJobExecutionException(string exceptionText) : base(nameof(PlanarJobExecutionException))
        {
            ExceptionText = exceptionText;
        }

        protected PlanarJobExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExceptionText = string.Empty;
        }

        public string ExceptionText { get; }
    }
}