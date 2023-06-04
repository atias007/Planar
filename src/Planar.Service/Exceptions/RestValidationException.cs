using Planar.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Exceptions
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    public sealed class RestValidationException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        public RestValidationException()
        {
        }

        public RestValidationException(string fieldName, string errorDetails)
        {
            Errors.Add(new RestProblem(fieldName, errorDetails));
        }

        public RestValidationException(string fieldName, string errorDetails, int errorCode)
        {
            Errors.Add(new RestProblem(fieldName, errorDetails, errorCode));
        }

        public RestValidationException(HashSet<RestProblem> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                throw new PlanarException("RestValidationException ctor fail. errors parameter is null or empty");
            }

            Errors = errors;
        }

        public HashSet<RestProblem> Errors { get; private set; } = new();

        public int TotalErrors
        {
            get
            {
                return Errors.SelectMany(e => e.Detail).Count();
            }
        }
    }
}