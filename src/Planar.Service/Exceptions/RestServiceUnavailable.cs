using System;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestServiceUnavailable : Exception
    {
        public RestServiceUnavailable(string message) : base(message)
        {
        }
    }
}