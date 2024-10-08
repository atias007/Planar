﻿using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CreatedResponseAttribute : SwaggerResponseAttribute
{
    public CreatedResponseAttribute(Type type) : base(201)
    {
        Type = type;
        ContentTypes = ["application/json"];
    }

    public CreatedResponseAttribute() : base(201)
    {
        Type = null;
    }
}