using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ConflictResponseAttribute()
    : ProducesResponseTypeAttribute(typeof(string), 409, MediaTypeNames.Text.Plain)
{
}