using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RequestTimeoutResponseAttribute : SwaggerResponseAttribute
{
    public RequestTimeoutResponseAttribute() : base(408)
    {
        Type = typeof(string);
        ContentTypes = ["plain/text"];
    }
}