using System;
using System.Collections.Generic;

namespace Planar;

public abstract class BaseProperties
{
    public static string? GetGlobalConfigPropertyPlaceholder(Func<string?> func, Dictionary<string, string?> parameters)
    {
        const string ph1 = "{{";
        const string ph2 = "}}";

        var value = func();
        if (string.IsNullOrWhiteSpace(value)) { return null; }
        if (!value.StartsWith(ph1) || !value.EndsWith(ph2)) { return null; }
        var key = value[ph1.Length..^ph2.Length].Trim();
        if (!parameters.TryGetValue(key, out string? newValue)) { return null; }

        return newValue ?? string.Empty;
    }
}