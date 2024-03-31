using RestSharp;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarRequestTimeoutException : BaseException
    {
        internal PlanarRequestTimeoutException(RestResponse response) : base(response)
        {
        }
    }
}