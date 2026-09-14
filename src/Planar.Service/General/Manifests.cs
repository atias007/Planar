using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Planar.Service.General;

public static class Manifest
{
    public const string Monitor = "monitor";
    public const string Job = "job";
    private static readonly Lock _locker = new();

    public static IEnumerable<string> Names => [Monitor, Job];

    public static bool IsValid(string name) => Names.Contains(name, StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string>? _manifests;

    public static FrozenDictionary<string, string> All
    {
        get
        {
            if (_manifests != null) { return _manifests; }
            lock (_locker)
            {
                if (_manifests != null) { return _manifests; }
                const string prefix = "Planar.Data.Manifests.";

                var assembly = Assembly.Load("Planar");
                var resources = assembly
                    .GetManifestResourceNames()
                    .Where(r => r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(r => new { Key = r[prefix.Length..], Value = GetManifestResource(assembly, r) });

                var jobTypes = ServiceUtil.JobTypes
                    .Select(t => new { Key = $"{t.Name}File.yml", Value = GetManifestResource(t.Assembly, $"{t.Name}.JobFile.yml") });

                _manifests = jobTypes.Union(resources).ToFrozenDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                return _manifests;
            }
        }
    }

    private static string GetManifestResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) { return string.Empty; }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}