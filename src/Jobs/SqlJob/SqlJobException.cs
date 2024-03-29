﻿using Planar.Common.Exceptions;
using System.Runtime.Serialization;

namespace Planar
{
    [Serializable]
    public class SqlJobException : PlanarException
    {
        public SqlJobException(string message) : base(message)
        {
        }

        public SqlJobException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SqlJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}