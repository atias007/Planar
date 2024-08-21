using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ServiceUnavailableResponseAttribute : SwaggerResponseAttribute
{
    public ServiceUnavailableResponseAttribute() : base(503)
    {
    }
}