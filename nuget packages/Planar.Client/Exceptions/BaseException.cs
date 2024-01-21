using RestSharp;
using System;

namespace Planar.Client.Exceptions
{
    public abstract class BaseException : Exception
    {
        protected BaseException(RestResponse response) : base($"Planar service return status code {response.StatusCode}")
        {
        }

        protected BaseException(string message) : base(message)
        {
        }

        protected BaseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}