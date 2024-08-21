using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class JsonConsumesAttribute : ConsumesAttribute
{
    public JsonConsumesAttribute() : base("application/json")
    {
    }
}