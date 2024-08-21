using Planar.Filters;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BadRequestResponseAttribute : SwaggerResponseAttribute
{
    public BadRequestResponseAttribute() : base(400)
    {
        Type = typeof(RestBadRequestResult);
        ContentTypes = ["application/json"];
    }
}