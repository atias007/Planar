using Planar.Job;

namespace HealthCheck;

internal class EndpointFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(Endpoint endpoint)
    {
        var key = GetKey(endpoint);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count));
        return count;
    }

    public void ResetFailCount(Endpoint endpoint)
    {
        var key = GetKey(endpoint);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(Endpoint endpoint)
    {
        return $"fail.count_{endpoint.Url}";
    }
}