using RestSharp;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarForbiddenException : BaseException
    {
        internal PlanarForbiddenException(RestResponse response) : base(response)
        {
        }
    }
}