using System;

namespace Planner.Client
{
    public class PlannerJobAggragateException : Exception
    {
        public PlannerJobAggragateException(string message)
            : base(message)
        {
        }
    }
}