namespace Common;

using Planar.Job;
using System.Globalization;

public class CheckSpanTracker(IBaseJob baseJob)
{
    public async Task<bool> IsSpanValid<T>(T entity)
        where T : BaseDefault, ICheckElement
    {
        return
            entity.AllowedFailSpan != null &&
            entity.AllowedFailSpan != TimeSpan.Zero &&
            entity.AllowedFailSpan > await LastFailSpan(entity);
    }

    private async Task<TimeSpan> LastFailSpan(ICheckElement element)
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

        await baseJob.PutJobDataAsync(key, DateTimeOffset.UtcNow.ToString(CultureInfo.CurrentCulture));
        return TimeSpan.Zero;
    }

    public async Task ResetFailSpan(ICheckElement element)
    {
        var key = GetKey(element);
        await baseJob.RemoveJobDataAsync(key);
    }

    private static string GetKey(ICheckElement element) => $"last.fail.{element.Key}";
}