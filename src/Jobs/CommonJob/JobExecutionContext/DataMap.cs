using System;
using System.Collections.Generic;

// *** DO NOT EDIT NAMESPACE IDENTETION ***
namespace Planar.Job
{
#if NETSTANDARD2_0

    internal class DataMap : Dictionary<string, string>, IDataMap
#else
    internal class DataMap : Dictionary<string, string?>, IDataMap
#endif
    {
        public DataMap()
        {
        }

#if NETSTANDARD2_0

        public DataMap(IDictionary<string, string> items)
#else
        public DataMap(IDictionary<string, string?>? items)
#endif
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
#if NETSTANDARD2_0
            if (!TryGetValue(key, out string value))
#else
            if (!TryGetValue(key, out string? value))
#endif
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

#if NETSTANDARD2_0

        public string Get(string key)
        {
            if (!TryGetValue(key, out string value))
            {
                throw new DataMapException($"Data with key '{key}' is not exists");
            }

            return value;
        }

#else
        public string? Get(string key)
        {
            if (!TryGetValue(key, out string? value))
            {
                throw new DataMapException($"Data with key '{key}' is not exists");
            }

            return value;
        }
#endif

        public bool TryGet<T>(string key, out T? value) where T : struct
        {
#if NETSTANDARD2_0
            if (!TryGetValue(key, out string tempValue))
            {
                value = default;
                return false;
            }
#else
            if (!TryGetValue(key, out string? tempValue))
            {
                value = default;
                return false;
            }
#endif

            try
            {
                var result = Convert.ChangeType(tempValue, typeof(T));
                value = tempValue == null ? default : (T?)result;
                return true;
            }
            catch
            {
                throw new DataMapException($"Data with key '{key}' and '{tempValue}' value and can't be converted to {typeof(T).Name} type");
            }
        }

#if NETSTANDARD2_0

        public bool TryGet(string key, out string value)
        {
            if (!TryGetValue(key, out string tempValue))
            {
                value = default;
                return false;
            }

            value = tempValue;
            return true;
        }

        public Dictionary<string, string> ToDictionary()
        {
            return this;
        }

#else
        public bool TryGet(string key, out string? value)
        {
            if (!TryGetValue(key, out string? tempValue))
            {
                value = default;
                return false;
            }

            value = tempValue;
            return true;
        }

        public Dictionary<string, string?> ToDictionary()
        {
            return this;
        }
#endif
    }
}