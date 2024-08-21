using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ConflictResponseAttribute : SwaggerResponseAttribute
{
    public ConflictResponseAttribute() : base(409, "text/plain")
    {
        Type = typeof(string);
        ContentTypes = ["text/plain"];
    }
}