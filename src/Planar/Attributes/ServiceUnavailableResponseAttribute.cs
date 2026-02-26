using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ServiceUnavailableResponseAttribute()
    : ProducesResponseTypeAttribute(503)
{
}