namespace Common;

using Planar.Job;
using System.Globalization;

public class CheckSpanTracker(IBaseJob baseJob)
{
    public bool IsSpanValid<T>(T entity)
        where T : BaseDefault, ICheckElement
    {
        return
            entity.Span != null &&
            entity.Span != TimeSpan.Zero &&
            entity.Span > LastFailSpan(entity);
    }

    private TimeSpan LastFailSpan(ICheckElement element)
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

    public void ResetFailSpan(ICheckElement element)
    {
        var key = GetKey(element);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(ICheckElement element) => $"last.fail.{element.Key}";
}