using System.Collections.Generic;

namespace Planar.Job;

public interface IDataMap : IReadOnlyDictionary<string, string?>
{
    T? Get<T>(string key) where T : struct;

    string? Get(string key);

    bool TryGet<T>(string key, out T? value) where T : struct;

    bool TryGet(string key, out string? value);

    bool Exists(string key);

    Dictionary<string, string?> ToDictionary();
}