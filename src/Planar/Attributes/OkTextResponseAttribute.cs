using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OkTextResponseAttribute : ProducesResponseTypeAttribute
{
    public OkTextResponseAttribute() : base(typeof(string), 200, MediaTypeNames.Text.Plain)
    {
    }
}