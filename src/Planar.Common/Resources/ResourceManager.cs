using System;
using System.IO;

namespace Planar.Common.Resources;

public enum ResourceMembers
{
    EmptyTable,
    Footer,
    Head,
    Header,
    Style
}

public static class ResourceManager
{
    public static string GetResource(ResourceMembers resource)
    {
        var assembly = typeof(ResourceManager).Assembly ??
           throw new InvalidOperationException("Assembly is null");

        var key = $"{nameof(Planar)}.{nameof(Common)}.{nameof(Resources)}.{resource}.html";

        using var stream = assembly.GetManifestResourceStream(key) ??
            throw new InvalidOperationException($"Resource '{key}' not found");
        using StreamReader reader = new(stream);
        var result = reader.ReadToEnd();
        return result;
    }
}