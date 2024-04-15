namespace Common;

using Planar.Job;

internal class CheckSpanTracker(IBaseJob baseJob)
{
    public TimeSpan LastFailSpan(ICheckElemnt element)
    {
        var key = GetKey(element);
        var span = TimeSpan.Zero;
        if (baseJob.Context.MergedJobDataMap.TryGet<DateTimeOffset>(key, out var value) && value != null)
        {
            span = DateTimeOffset.UtcNow.Subtract(value.GetValueOrDefault());
        }

        baseJob.PutJobData(key, DateTimeOffset.UtcNow);
        return span;
    }

    public void ResetFailCount(ICheckElemnt element)
    {
        var key = GetKey(element);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElemnt element) => $"last.fail.{element.Key}";
}