using Planar.Job;

namespace Common;

public class FinalayzeDetails(IBaseJob baseJob, bool success)
{
    public string Key { get; } = baseJob.Context.JobDetails.Key.ToString() ?? string.Empty;
    public int? EffectedRows { get; } = baseJob.EffectedRows;

    public bool Success { get; } = success;

    public string FireInstanceId { get; } = baseJob.Context.FireInstanceId;

    public DateTimeOffset FireTime { get; } = baseJob.Context.FireTime;
}

public sealed class FinalayzeDetails<T>(T data, IBaseJob baseJob, bool success) : FinalayzeDetails(baseJob, success)
{
    public T Data { get; } = data;
}