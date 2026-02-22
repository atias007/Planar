using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;

namespace Planar.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RequestTimeoutResponseAttribute : ProducesResponseTypeAttribute
{
    public RequestTimeoutResponseAttribute() : base(typeof(string), 408, MediaTypeNames.Text.Plain)
    {
    }
}