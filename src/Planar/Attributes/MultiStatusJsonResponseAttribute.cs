using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class MultiStatusJsonResponseAttribute : ProducesResponseTypeAttribute
{
    public MultiStatusJsonResponseAttribute() : base(207)
    {
    }

    public MultiStatusJsonResponseAttribute(Type type) : base(type, 207, MediaTypeNames.Application.Json)
    {
    }
}