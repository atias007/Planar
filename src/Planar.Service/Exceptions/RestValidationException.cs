using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    public class RestValidationException : Exception
    {
        public RestValidationException()
        {
        }

        public RestValidationException(string fieldName, string errorDetails)
        {
            Errors.Add(new RestProblem(fieldName, errorDetails));
        }

        protected RestValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // ...
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