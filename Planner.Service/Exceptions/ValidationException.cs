using System;

namespace Planner.Service.Exceptions
{
    public class PlannerValidationException : Exception
    {
        public PlannerValidationException(string message) : base(message)
        {
        }

        public PlannerValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}