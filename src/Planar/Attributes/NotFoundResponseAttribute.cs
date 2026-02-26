using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class NotFoundResponseAttribute : ProducesResponseTypeAttribute
{
    public NotFoundResponseAttribute() : base(typeof(string), 404, MediaTypeNames.Text.Plain)
    {
    }
}