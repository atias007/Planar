using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal static class Extensions
    {
        public static void AddOrUpdate<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        public static Dictionary<string, string?> Merge(this Dictionary<string, string?> dictionary, Dictionary<string, string?> source)
        {
            var merge = MergeInner(dictionary, source);
            var result = new Dictionary<string, string?>(merge);
            return result;
        }

        public static SortedDictionary<string, string?> Merge(this SortedDictionary<string, string?> dictionary, SortedDictionary<string, string?> source)
        {
            var merge = MergeInner(dictionary, source);
            var result = new SortedDictionary<string, string?>(merge);
            return result;
        }

        private static IDictionary<string, string?> MergeInner(IDictionary<string, string?> dictionary, IDictionary<string, string?> source)
        {
            if (source == null) { return dictionary; }
            dictionary ??= new SortedDictionary<string, string?>();

            foreach (var item in source)
            {
                if (dictionary.ContainsKey(item.Key))
                {
                    dictionary[item.Key] = item.Value;
                }
                else
                {
                    dictionary.Add(item.Key, item.Value);
                }
            }

            return dictionary;
        }
    }
}