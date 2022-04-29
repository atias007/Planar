namespace Planar.Service.Exceptions
{
    public class NotExistsException : PlanarValidationException
    {
        public NotExistsException(string message) : base(message)
        {
        }
    }
}