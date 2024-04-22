namespace Common;

using Planar.Job;
using System.Globalization;

public class CheckSpanTracker(IBaseJob baseJob)
{
    public TimeSpan LastFailSpan(ICheckElemnt element)
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

    public void ResetFailSpan(ICheckElemnt element)
    {
        var key = GetKey(element);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElemnt element) => $"last.fail.{element.Key}";
}