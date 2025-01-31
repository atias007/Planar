using System.Collections.Generic;

// *** DO NOT EDIT NAMESPACE IDENTETION ***
namespace Planar.Job
{
#if NETSTANDARD2_0

    public interface IDataMap : IReadOnlyDictionary<string, string>
    {
        string Get(string key);

        bool TryGet(string key, out string value);

        Dictionary<string, string> ToDictionary();

#else
    public interface IDataMap : IReadOnlyDictionary<string, string?>
    {
        string? Get(string key);
        bool TryGet(string key, out string? value);
        Dictionary<string, string?> ToDictionary();

#endif

        T? Get<T>(string key) where T : struct;

        bool TryGet<T>(string key, out T? value) where T : struct;

        bool Exists(string key);
    }
}