using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Planar.Filters
{
    public class RequestTimeoutObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status408RequestTimeout;

        public RequestTimeoutObjectResult(object value) : base(value)
        {
            StatusCode = DefaultStatusCode;
        }
    }
}