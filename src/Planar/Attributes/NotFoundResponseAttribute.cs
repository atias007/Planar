using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes
{
    public class NotFoundResponseAttribute : SwaggerResponseAttribute
    {
        public NotFoundResponseAttribute() : base(404)
        {
            Type = typeof(string);
            ContentTypes = new[] { "plain/text" };
        }
    }
}