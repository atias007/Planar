using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class NotFoundResponseAttribute : SwaggerResponseAttribute
{
    public NotFoundResponseAttribute() : base(404)
    {
        Type = typeof(string);
        ContentTypes = ["plain/text"];
    }
}