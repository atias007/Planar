using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class NoContentResponseAttribute : ProducesResponseTypeAttribute
{
    public NoContentResponseAttribute() : base(204)
    {
    }
}