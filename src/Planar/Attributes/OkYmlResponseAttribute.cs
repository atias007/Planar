using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OkYmlResponseAttribute : ProducesResponseTypeAttribute
{
    public OkYmlResponseAttribute() : base(typeof(string), 200, "text/yaml")
    {
    }
}