using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class JsonConsumesAttribute : ConsumesAttribute
{
    public JsonConsumesAttribute() : base(MediaTypeNames.Application.Json)
    {
    }
}