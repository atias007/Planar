namespace Common;

using Planar.Job;
using System.Diagnostics;
using System.Globalization;

public class CheckIntervalTracker(IBaseJob baseJob)
{
    public bool ShouldRun(ICheckElement element, TimeSpan interval)
    {
        var key = GetKey(element);
        var lastSpan = GetLastRunningSpan(key);
        if (lastSpan > TimeSpan.Zero && lastSpan < interval)
        {
            return false;
        }

        SetLastRunning(key);
        return true;
    }

    private TimeSpan GetLastRunningSpan(string key)
    {
        if (baseJob.Context.MergedJobDataMap.TryGet(key, out var value)
            && value != null
            && DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, out var date))
        {
            var now = ResetMilliseconds(FireTime);
            var span = now.Subtract(date);
            return span;
        }

        SetLastRunning(key);
        return TimeSpan.Zero;
    }

    private void SetLastRunning(string key, DateTimeOffset? now = null)
    {
        var date = now ?? ResetMilliseconds(FireTime);
        baseJob.PutJobData(key, date.ToString(CultureInfo.CurrentCulture));
    }

    private DateTimeOffset FireTime => baseJob.Context.ScheduledFireTime ?? baseJob.Context.FireTime;

    private static DateTimeOffset ResetMilliseconds(DateTimeOffset dateTimeOffset)
    {
        return new DateTimeOffset(
            dateTimeOffset.Year,
            dateTimeOffset.Month,
            dateTimeOffset.Day,
            dateTimeOffset.Hour,
            dateTimeOffset.Minute,
            dateTimeOffset.Second,
            dateTimeOffset.Offset
        );
    }

    private static string GetKey(ICheckElement element) => $"last.running.{element.Key}";
}