using System.Collections.Generic;

namespace Planar.Job
{
    internal static class Extensions
    {
        public static Dictionary<string, string?> Merge(this Dictionary<string, string?> source, IDictionary<string, string?> target)
        {
            if (target == null) { return source; }
            source ??= new Dictionary<string, string?>();

            foreach (var item in target)
            {
                if (source.ContainsKey(item.Key))
                {
                    source[item.Key] = item.Value;
                }
                else
                {
                    source.Add(item.Key, item.Value);
                }
            }

            return source;
        }
    }
}