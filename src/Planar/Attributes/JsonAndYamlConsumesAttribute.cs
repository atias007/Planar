using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class JsonAndYamlConsumesAttribute : ConsumesAttribute
{
    public JsonAndYamlConsumesAttribute() : base(MediaTypeNames.Application.Json, MediaTypeNames.Application.Yaml)
    {
    }
}