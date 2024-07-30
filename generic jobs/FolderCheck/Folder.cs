using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Folder : BaseDefault, INamedCheckElement
{
    public Folder(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string>("name") ?? string.Empty;
        Path = section.GetValue<string>("path") ?? string.Empty;
        FilesPattern = section.GetValue<string?>("files pattern")?.Split(',').ToList();
        IncludeSubdirectories = section.GetValue<bool>("include subdirectories");
        TotalSize = section.GetValue<string?>("total size");
        FileSize = section.GetValue<string?>("file size");
        FileCount = section.GetValue<int?>("file count");
        CreatedAge = section.GetValue<string?>("created age");
        ModifiedAge = section.GetValue<string?>("modified age");
        Active = section.GetValue<bool?>("active") ?? true;

        TotalSizeNumber = CommonUtil.GetSize(TotalSize, "total size");
        FileSizeNumber = CommonUtil.GetSize(FileSize, "file size");
        CreatedAgeDate = CommonUtil.GetDateFromSpan(CreatedAge, "created age");
        ModifiedAgeDate = CommonUtil.GetDateFromSpan(ModifiedAge, "modified age");
        IsAbsolutePath = System.IO.Path.IsPathFullyQualified(Path);
    }

    public string Name { get; private set; }
    public string Path { get; private set; }
    public IEnumerable<string>? FilesPattern { get; private set; }
    public bool IncludeSubdirectories { get; private set; }
    public string? TotalSize { get; private set; }
    public string? FileSize { get; private set; }
    public int? FileCount { get; private set; }
    public string? CreatedAge { get; private set; }
    public string? ModifiedAge { get; private set; }
    public bool Active { get; private set; }

    //// --------------------------------------- ////

    public long? TotalSizeNumber { get; }
    public long? FileSizeNumber { get; }
    public DateTime? CreatedAgeDate { get; }
    public DateTime? ModifiedAgeDate { get; }
    public string Key => Name;
    public bool IsAbsolutePath { get; }
    public bool IsRelativePath => !IsAbsolutePath;

    public void SetDefaultFilePattern()
    {
        FilesPattern = new List<string> { "*.*" };
    }

    public bool IsValid =>
        TotalSizeNumber != null || FileSizeNumber != null || FileCount != null || CreatedAgeDate != null || ModifiedAgeDate != null;

    public string GetFullPath(string? host)
    {
        return IsAbsolutePath || string.IsNullOrWhiteSpace(host) ? Path : PathCombine(host, Path);
    }

    //// --------------------------------------- ////

    private static string PathCombine(string part1, string part2)
    {
        part1 = part1.Trim().TrimEnd(System.IO.Path.DirectorySeparatorChar);
        part2 = part2.Trim().TrimStart(System.IO.Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(part1)) { return part2; }
        if (string.IsNullOrWhiteSpace(part2)) { return part1; }
        return $"{part1}{System.IO.Path.DirectorySeparatorChar}{part2}";
    }
}