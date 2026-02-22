using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OkJsonResponseAttribute : ProducesResponseTypeAttribute
{
    public OkJsonResponseAttribute() : base(200)
    {
    }

    public OkJsonResponseAttribute(Type type) : base(type, 200, MediaTypeNames.Application.Json)
    {
    }
}