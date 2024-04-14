using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Folder : BaseDefault, IFolder, ICheckElemnt
{
    public Folder(IConfigurationSection section, string path)
    {
        Name = section.GetValue<string?>("name");
        Path = path;
        FilesPattern = section.GetValue<string?>("files pattern")?.Split(',').ToList();
        IncludeSubdirectories = section.GetValue<bool>("include subdirectories");
        TotalSize = section.GetValue<string?>("total size");
        FileSize = section.GetValue<string?>("file size");
        FileCount = section.GetValue<int?>("file count");
        CreatedAge = section.GetValue<string?>("created age");
        ModifiedAge = section.GetValue<string?>("modified age");

        //// --------------------------------------- ////

        RetryCount = section.GetValue<int?>("retry count");
        RetryInterval = section.GetValue<TimeSpan?>("retry interval");
        MaximumFailsInRow = section.GetValue<int?>("maximum fails in row");
    }

    public string? Name { get; set; }
    public string Path { get; private set; }
    public IEnumerable<string>? FilesPattern { get; set; }
    public bool IncludeSubdirectories { get; set; }
    public string? TotalSize { get; set; }
    public string? FileSize { get; set; }
    public int? FileCount { get; set; }
    public string? CreatedAge { get; set; }
    public string? ModifiedAge { get; set; }

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