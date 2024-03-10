using System;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public sealed class RestForbiddenException : Exception
    {
        public RestForbiddenException()
        {
        }
    }
}