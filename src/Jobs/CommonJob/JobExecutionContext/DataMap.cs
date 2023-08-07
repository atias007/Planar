using System;
using System.Collections.Generic;

namespace Planar.Job
{
    internal class DataMap : Dictionary<string, string?>, IDataMap
    {
        public DataMap()
        {
        }

        public DataMap(IDictionary<string, string?>? items)
        {
            if (items == null) { return; }
            foreach (var i in items)
            {
                Add(i.Key, i.Value);
            }
        }

        public bool Exists(string key)
        {
            return ContainsKey(key);
        }

        public T? Get<T>(string key) where T : struct
        {
            if (!TryGetValue(key, out string? value))
            {
                throw new DataMapException($"Data with key '{key}' is not exists");
            }

            try
            {
                var result = Convert.ChangeType(value, typeof(T));
                return
                    result == null ? default : (T)result;
            }
            catch
            {
                throw new DataMapException($"Data with key '{key}' and '{value}' value and can't be converted to {typeof(T).Name} type");
            }
        }

        public string? Get(string key)
        {
            if (!TryGetValue(key, out string? value))
            {
                throw new DataMapException($"Data with key '{key}' is not exists");
            }

            return value;
        }
    }
}