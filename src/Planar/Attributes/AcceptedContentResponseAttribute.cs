using Microsoft.AspNetCore.Mvc;
using System;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class AcceptedContentResponseAttribute : ProducesResponseTypeAttribute
{
    public AcceptedContentResponseAttribute() : base(202)
    {
    }
}