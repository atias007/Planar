namespace Common;

using Planar.Job;
using System.Globalization;

internal class CheckSpanTracker(IBaseJob baseJob)
{
    public TimeSpan LastFailSpan(ICheckElemnt element, string? additionalKey = null)
    {
        var key = GetKey(element, additionalKey);
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

    public void ResetFailSpan(ICheckElemnt element, string? additionalKey = null)
    {
        var key = GetKey(element, additionalKey);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElemnt element, string? additionalKey) => $"last.fail.{element.Key}.{additionalKey}";
}