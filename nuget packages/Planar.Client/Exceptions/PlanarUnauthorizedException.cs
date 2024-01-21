using RestSharp;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarUnauthorizedException : BaseException
    {
        internal PlanarUnauthorizedException(RestResponse response) : base(response)
        {
        }
    }
}