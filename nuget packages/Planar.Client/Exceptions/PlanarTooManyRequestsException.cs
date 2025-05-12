namespace Planar.Client.Exceptions
{
    public sealed class PlanarTooManyRequestsException : BaseException
    {
        internal PlanarTooManyRequestsException(RestResponse response) : base(response)
        { }
    }
}