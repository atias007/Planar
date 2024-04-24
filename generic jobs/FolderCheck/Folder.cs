using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Folder(IConfigurationSection section, string path) : BaseDefault(section), INamedCheckElement
{
    public string Name { get; set; } = section.GetValue<string>("name") ?? string.Empty;
    public string Path { get; private set; } = path;
    public IEnumerable<string>? FilesPattern { get; private set; } = section.GetValue<string?>("files pattern")?.Split(',').ToList();
    public bool IncludeSubdirectories { get; private set; } = section.GetValue<bool>("include subdirectories");
    public string? TotalSize { get; private set; } = section.GetValue<string?>("total size");
    public string? FileSize { get; private set; } = section.GetValue<string?>("file size");
    public int? FileCount { get; private set; } = section.GetValue<int?>("file count");
    public string? CreatedAge { get; private set; } = section.GetValue<string?>("created age");
    public string? ModifiedAge { get; private set; } = section.GetValue<string?>("modified age");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    //// --------------------------------------- ////

    public long? TotalSizeNumber { get; private set; }

    public long? FileSizeNumber { get; private set; }
    public DateTime? CreatedAgeDate { get; private set; }
    public DateTime? ModifiedAgeDate { get; private set; }
    public string Key => Name;

    public void SetDefaultFilePattern()
    {
        FilesPattern = new List<string> { "*.*" };
    }

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