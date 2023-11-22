using System;
using System.Collections.Generic;

namespace Planar
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    [Serializable]
    public class PlanarJobAggragateException : AggregateException
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
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