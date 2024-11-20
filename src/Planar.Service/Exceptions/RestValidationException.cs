using Planar.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Exceptions
{
    public sealed class RestValidationException : Exception
    {
        public RestValidationException()
        {
        }

        public RestValidationException(string fieldName, string errorDetails) : base(errorDetails)
        {
            Errors.Add(new RestProblem(fieldName, errorDetails));
        }

        public RestValidationException(string fieldName, string errorDetails, int errorCode) : base(errorDetails)
        {
            Errors.Add(new RestProblem(fieldName, errorDetails, errorCode));
        }

        public RestValidationException(string fieldName, string errorDetails, string clientMessage, string? suggestion = null) : base(errorDetails)

        {
            Errors.Add(new RestProblem(fieldName, errorDetails));

            ClientMessage = clientMessage;
            Suggestion = suggestion;
        }

        public RestValidationException(HashSet<RestProblem> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                throw new PlanarException("RestValidationException ctor fail. errors parameter is null or empty");
            }

            Errors = errors;
        }

        public HashSet<RestProblem> Errors { get; private set; } = [];

        public string? ClientMessage { get; private set; }
        public string? Suggestion { get; private set; }

        public int TotalErrors
        {
            get
            {
                return Errors.SelectMany(e => e.Detail).Count();
            }
        }
    }
}