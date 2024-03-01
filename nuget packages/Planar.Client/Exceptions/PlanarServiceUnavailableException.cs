using RestSharp;
using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarServiceUnavailableException : BaseException
    {
        internal PlanarServiceUnavailableException(RestResponse response) : base(response)
        {
        }
    }
}