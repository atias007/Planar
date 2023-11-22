using System;

namespace Planar.Common.Exceptions
{
    public class PlanarJobExecutionException : Exception
    {
        public PlanarJobExecutionException(string message) : base(message)
        {
        }

        public string? ExceptionText { get; set; }

        public string? MostInnerMessage { get; set; }

        public string? MostInnerExceptionText { get; set; }
    }
}