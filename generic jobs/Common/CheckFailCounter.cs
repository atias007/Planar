namespace Common;

using Planar.Job;
using System.Globalization;

public class CheckFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(ICheckElement element)
    {
        var key = GetKey(element);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count, CultureInfo.InvariantCulture));
        return count;
    }

    public void ResetFailCount(ICheckElement element)
    {
        var key = GetKey(element);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElement element) => $"fail.count.{element.Key}";
}