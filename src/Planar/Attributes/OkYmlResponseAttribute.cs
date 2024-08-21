using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OkYmlResponseAttribute : SwaggerResponseAttribute
{
    public OkYmlResponseAttribute() : base(200)
    {
        Type = typeof(string);
        ContentTypes = ["text/yaml"];
    }
}