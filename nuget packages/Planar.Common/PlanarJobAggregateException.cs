using System;
using System.Collections.Generic;

namespace Planar
{
    public sealed class PlanarJobAggregateException : AggregateException
    {
        public PlanarJobAggregateException(string message)
            : base(message)
        {
        }

        public PlanarJobAggregateException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }
    }
}