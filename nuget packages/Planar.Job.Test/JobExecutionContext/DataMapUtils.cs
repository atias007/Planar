using System.Collections.Generic;

namespace Planar.Job.Test.JobExecutionContext
{
    internal static class DataMapUtils
    {
        public static SortedDictionary<string, string> Convert(Dictionary<string, object> source)
        {
            var result = new SortedDictionary<string, string>();
            if (source == null) { return result; }

            foreach (var item in source)
            {
                var strValue = PlanarConvert.ToString(item.Value);
                result.TryAdd(item.Key, strValue);
            }

            return result;
        }
    }
}