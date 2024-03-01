using RestSharp;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarNotFoundException : BaseException
    {
        internal PlanarNotFoundException(RestResponse response) : base(response)
        {
        }

        internal PlanarNotFoundException(string message) : base(message)
        {
        }
    }
}