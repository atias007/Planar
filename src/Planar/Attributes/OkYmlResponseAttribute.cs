using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes;

public class OkYmlResponseAttribute : SwaggerResponseAttribute
{
    public OkYmlResponseAttribute() : base(200)
    {
        Type = typeof(string);
        ContentTypes = ["text/yaml"];
    }
}