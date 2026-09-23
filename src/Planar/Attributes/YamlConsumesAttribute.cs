using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class YamlConsumesAttribute : ConsumesAttribute
{
    public YamlConsumesAttribute() : base(MediaTypeNames.Application.Yaml)
    {
    }
}