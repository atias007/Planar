using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OkTextResponseAttribute : SwaggerResponseAttribute
{
    public OkTextResponseAttribute() : base(200)
    {
        Type = typeof(string);
        ContentTypes = ["plain/text"];
    }
}