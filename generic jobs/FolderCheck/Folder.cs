using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Folder(IConfigurationSection section, string path) : IFolder, ICheckElemnt
{
    public string? Name { get; set; } = section.GetValue<string>("name");
    public string Path { get; private set; } = path;
    public IEnumerable<string>? FilesPattern { get; set; } = section.GetValue<string>("files pattern")?.Split(',').ToList();
    public bool IncludeSubdirectories { get; set; } = section.GetValue<bool>("include subdirectories");
    public string? TotalSize { get; set; } = section.GetValue<string>("total size");
    public string? FileSize { get; set; } = section.GetValue<string>("file size");
    public int? FileCount { get; set; } = section.GetValue<int?>("file count");
    public string? CreatedAge { get; set; } = section.GetValue<string>("created age");
    public string? ModifiedAge { get; set; } = section.GetValue<string>("modified age");

    //// --------------------------------------- ////
    public int? RetryCount { get; set; } = section.GetValue<int?>("retry count");

    public TimeSpan? RetryInterval { get; set; } = section.GetValue<TimeSpan?>("retry interval");
    public int? MaximumFailsInRow { get; set; } = section.GetValue<int?>("maximum fails in row");

    //// --------------------------------------- ////
    public long? TotalSizeNumber { get; set; }

    public long? FileSizeNumber { get; set; }
    public DateTime? CreatedAgeDate { get; set; }
    public DateTime? ModifiedAgeDate { get; set; }
    public string Key => Path;

    public bool IsValid()
    {
        return TotalSizeNumber != null && FileSizeNumber != null && FileCount != null && CreatedAgeDate != null && ModifiedAgeDate != null;
    }

    //// --------------------------------------- ////
    public void SetMonitorArguments()
    {
        TotalSizeNumber = CommonUtil.GetSize(TotalSize, "total size");
        FileSizeNumber = CommonUtil.GetSize(FileSize, "file size");
        CreatedAgeDate = CommonUtil.GetDateFromSpan(CreatedAge, "created age");
        ModifiedAgeDate = CommonUtil.GetDateFromSpan(ModifiedAge, "modified age");
    }
}