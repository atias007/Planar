using System;
using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes;

public class OkJsonResponseAttribute : SwaggerResponseAttribute
{
    public OkJsonResponseAttribute() : base(200)
    {
    }

    public OkJsonResponseAttribute(Type type) : base(200)
    {
        Type = type;
        ContentTypes = ["application/json"];
    }
}