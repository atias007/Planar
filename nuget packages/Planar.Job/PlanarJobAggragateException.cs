using System;
using System.Collections.Generic;

namespace Planar
{
    public sealed class PlanarJobAggragateException : AggregateException
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }

        public PlanarJobAggragateException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }
    }
}