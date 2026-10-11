using Planar.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonJob;

internal class CommonJobUtil(IJobPropertyDataLayer dal)
{
    public async Task<IEnumerable<string>> GetJobsUsingGlobalParam(string key)
    {
        var yamls = await dal.GetJobsGlobalConfigs();
        var result = new List<string>();
        foreach (var item in yamls)
        {
            if (string.IsNullOrWhiteSpace(item)) { continue; }

            var keys = YmlUtil.Deserialize<IEnumerable<string>>(item);
            if (keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(item);
            }
        }

        return result;
    }

    ////public async Task<IEnumerable<string>> GetJobsUsingResource(string resourceName)
    ////{
    ////    serviceutil
    ////}
}