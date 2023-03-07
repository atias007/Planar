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

            var message = $"Expect exception of type {typeof(TException).FullName} there is exception of type {_result.Exception.GetType().FullName}";
            throw new AssertPlanarException(message);
        }

        private static bool HasExceptionType<TException>(Exception ex)
            where TException : Exception
        {
            if (ex is TException) { return true; }
            if (ex.InnerException == null) { return false; }
            return HasExceptionType<TException>(ex.InnerException);
        }

        private static bool HasExceptionType<TException>(AggregateException ex)
            where TException : Exception
        {
            var all = ex.Flatten().InnerExceptions;

            foreach (var item in all)
            {
                if (HasExceptionType<TException>(item)) { return true; }
            }

            return false;
        }
    }
}