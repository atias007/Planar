namespace Common;

using Planar.Job;

internal class CheckFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(ICheckElemnt element)
    {
        var key = GetKey(element);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count));
        return count;
    }

    public void ResetFailCount(ICheckElemnt element)
    {
        var key = GetKey(element);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElemnt element)
    {
        return $"fail.count_{element.Key}";
    }
}