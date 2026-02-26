using Microsoft.AspNetCore.Mvc;
using Planar.Filters;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BadRequestResponseAttribute() :
    ProducesResponseTypeAttribute(typeof(RestBadRequestResult), 400)
{
}