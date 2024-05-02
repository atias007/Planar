namespace Common;

using Planar.Job;
using System.Globalization;

public class CheckIntervalTracker(IBaseJob baseJob)
{
    public TimeSpan LastRunningSpan(ICheckElement element)
    {
        var key = GetKey(element);
        if (baseJob.Context.MergedJobDataMap.TryGet(key, out var value)
            && value != null
            && DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, out var date)
            )
        {
            var span = DateTimeOffset.UtcNow.Subtract(date);
            return span;
        }

        baseJob.PutJobData(key, DateTimeOffset.UtcNow.ToString(CultureInfo.CurrentCulture));
        return TimeSpan.Zero;
    }

    public void SetLastRunning(ICheckElement element)
    {
        var key = GetKey(element);
        baseJob.PutJobData(key, DateTimeOffset.UtcNow.ToString(CultureInfo.CurrentCulture));
    }

    private static string GetKey(ICheckElement element) => $"last.running.{element.Key}";
}