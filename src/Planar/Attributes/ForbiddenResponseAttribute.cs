using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ForbiddenResponseAttribute : ProducesResponseTypeAttribute
{
    public ForbiddenResponseAttribute() : base(403)
    {
    }
}