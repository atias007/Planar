namespace Common;

using Planar.Job;

internal class CheckFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(ICheckElemnt element, string? additionalKey = null)
    {
        var key = GetKey(element, additionalKey);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count));
        return count;
    }

    public void ResetFailCount(ICheckElemnt element, string? additionalKey = null)
    {
        var key = GetKey(element, additionalKey);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElemnt element, string? additionalKey) => $"fail.count.{element.Key}.{additionalKey}";
}