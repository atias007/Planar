using System;

namespace Planar.API.Common;

[AttributeUsage(AttributeTargets.Property)]
public class DisplayFormatAttribute(string format) : Attribute
{
    public string Format { get; } = format;
}