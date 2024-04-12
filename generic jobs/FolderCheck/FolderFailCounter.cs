using Planar.Job;

namespace FolderCheck;

internal class FolderFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(Folder folder)
    {
        var key = GetKey(folder);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count));
        return count;
    }

    public void ResetFailCount(Folder folder)
    {
        var key = GetKey(folder);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(Folder folder)
    {
        return $"fail.count_{folder.Path}";
    }
}