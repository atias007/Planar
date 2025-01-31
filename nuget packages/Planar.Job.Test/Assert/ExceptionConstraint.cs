using Planar.Job.Test.Common;
using System;
using System.Text.RegularExpressions;

namespace Planar.Job.Test.Assert
{
    public class ExceptionConstraint : AssertPlanarConstraint
    {
        internal ExceptionConstraint(IJobExecutionResult result) : base(result)
        {
        }

        public AssertPlanarConstraint IsNull()
        {
            if (_result.Exception == null) { return Assert; }

            var message = $"Expect exception to be null but there is exception of type {_result.Exception.GetType().FullName}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotNull()
        {
            if (_result.Exception != null) { return Assert; }

            var message = "Expect exception to be not null but the exception is null";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint OfType<TException>()
            where TException : Exception
        {
            if (_result.Exception is AggregateException)
            {
                var aggEx = _result.Exception as AggregateException;
                if (HasExceptionType<TException>(aggEx)) { return Assert; }
            }

            if (HasExceptionType<TException>(_result.Exception)) { return Assert; }

            var message = $"Expect exception of type {typeof(TException).FullName} there is exception of type {_result.Exception?.GetType().FullName}";
            throw new AssertPlanarException(message);
        }

#if NETSTANDARD2_0

        private static bool HasExceptionType<TException>(Exception ex)

#else
        private static bool HasExceptionType<TException>(Exception? ex)

#endif
            where TException : Exception
        {
            if (ex == null) { return false; }
            if (ex is TException) { return true; }
            if (ex.InnerException == null) { return false; }
            return HasExceptionType<TException>(ex.InnerException);
        }

#if NETSTANDARD2_0

        private static bool HasExceptionType<TException>(AggregateException ex)
#else
        private static bool HasExceptionType<TException>(AggregateException? ex)
#endif
            where TException : Exception
        {
            if (ex == null) { return false; }

            var all = ex.Flatten().InnerExceptions;

            foreach (var item in all)
            {
                if (HasExceptionType<TException>(item)) { return true; }
            }

            return false;
        }
    }
}