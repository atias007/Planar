using Planar.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes
{
    public class BadRequestResponseAttribute : SwaggerResponseAttribute
    {
        public BadRequestResponseAttribute() : base(400)
        {
            Type = typeof(RestBadRequestResult);
            ContentTypes = new[] { "application/json" };
        }
    }
}