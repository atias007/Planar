using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes
{
    public class ConflictResponseAttribute : SwaggerResponseAttribute
    {
        public ConflictResponseAttribute() : base(409, "text/plain")
        {
            Type = typeof(string);
            ContentTypes = new[] { "text/plain" };
        }
    }
}