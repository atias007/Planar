using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CreatedResponseAttribute
    : ProducesResponseTypeAttribute
{
    public CreatedResponseAttribute(Type type) : base(type, 201, MediaTypeNames.Application.Json)
    {
    }

    public CreatedResponseAttribute() : base(201)
    {
    }
}