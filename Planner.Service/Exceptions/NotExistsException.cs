namespace Planner.Service.Exceptions
{
    public class NotExistsException : PlannerValidationException
    {
        public NotExistsException(string message) : base(message)
        {
        }
    }
}