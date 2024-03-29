using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes
{
    public class RequestTimeoutResponseAttribute : SwaggerResponseAttribute
    {
        public RequestTimeoutResponseAttribute() : base(408)
        {
            Type = typeof(string);
            ContentTypes = ["plain/text"];
        }
    }
}