namespace Planar.Client.Exceptions
{
    public sealed class PlanarConflictException : BaseException
    {
        internal PlanarConflictException(RestResponse response) : base(response)
        {
        }

        internal PlanarConflictException(string message) : base(message)
        {
        }
    }
}