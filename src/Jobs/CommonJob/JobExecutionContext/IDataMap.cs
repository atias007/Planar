using System.Collections.Generic;

namespace Planar.Job
{
    public interface IDataMap : IReadOnlyDictionary<string, string?>
    {
        T? Get<T>(string key) where T : struct;

        string? Get(string key);

        bool Exists(string key);
    }
}