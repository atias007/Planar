using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes;

public class OkTextResponseAttribute : SwaggerResponseAttribute
{
    public OkTextResponseAttribute() : base(200)
    {
        Type = typeof(string);
        ContentTypes = ["plain/text"];
    }
}