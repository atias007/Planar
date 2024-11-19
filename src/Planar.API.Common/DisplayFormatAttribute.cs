using System;

namespace Planar.API.Common;

public enum SpecialFormat
{
    None,
    Log,
    Duration
}

[AttributeUsage(AttributeTargets.Property)]
public class DisplayFormatAttribute : Attribute
{
    public string? Format { get; set; }

    public string? DisplayName { get; set; }

    public int MaximumChars { get; set; }

    public SpecialFormat SpecialFormat { get; set; }
}