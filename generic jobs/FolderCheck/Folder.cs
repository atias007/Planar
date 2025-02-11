using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Folder : BaseDefault, INamedCheckElement, IVetoEntity
{
    public Folder(Folder source) : base(source)
    {
        Name = source.Name;
        HostGroupName = source.HostGroupName;
        Path = source.Path;
        FilesPattern = source.FilesPattern;
        IncludeSubdirectories = source.IncludeSubdirectories;
        TotalSize = source.TotalSize;
        FileSize = source.FileSize;
        FileCount = source.FileCount;
        CreatedAge = source.CreatedAge;
        ModifiedAge = source.ModifiedAge;

        TotalSizeNumber = source.TotalSizeNumber;
        FileSizeNumber = source.FileSizeNumber;
        CreatedAgeDate = source.CreatedAgeDate;
        ModifiedAgeDate = source.ModifiedAgeDate;
        IsAbsolutePath = source.IsAbsolutePath;

        if (FilesPattern == null || !FilesPattern.Any())
        {
            FilesPattern = new List<string> { "*.*" };
        }
    }

    public Folder(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        Path = section.GetValue<string>("path") ?? string.Empty;
        FilesPattern = section.GetSection("files pattern").Get<string[]?>();
        IncludeSubdirectories = section.GetValue<bool>("include subdirectories");
        TotalSize = section.GetValue<string?>("total size");
        FileSize = section.GetValue<string?>("file size");
        FileCount = section.GetValue<int?>("file count");
        CreatedAge = section.GetValue<string?>("created age");
        ModifiedAge = section.GetValue<string?>("modified age");

        TotalSizeNumber = CommonUtil.GetSize(TotalSize, "total size");
        FileSizeNumber = CommonUtil.GetSize(FileSize, "file size");
        CreatedAgeDate = CommonUtil.GetDateFromSpan(CreatedAge, "created age");
        ModifiedAgeDate = CommonUtil.GetDateFromSpan(ModifiedAge, "modified age");
        IsAbsolutePath = System.IO.Path.IsPathFullyQualified(Path);
    }

    public string Name { get; private set; }
    public string? HostGroupName { get; private set; }
    public string Path { get; private set; }
    public IEnumerable<string>? FilesPattern { get; private set; }
    public bool IncludeSubdirectories { get; private set; }
    public string? TotalSize { get; private set; }
    public string? FileSize { get; private set; }
    public int? FileCount { get; private set; }
    public string? CreatedAge { get; private set; }
    public string? ModifiedAge { get; private set; }

    public FolderResult Result { get; } = new();

    //// --------------------------------------- ////

    public long? TotalSizeNumber { get; }
    public long? FileSizeNumber { get; }
    public DateTime? CreatedAgeDate { get; }
    public DateTime? ModifiedAgeDate { get; }
    public string Key => Name;
    public bool IsAbsolutePath { get; }
    public bool IsRelativePath => !IsAbsolutePath;

    // internal use for relative urls
    public string? Host { get; set; }

    //// -------------------------- ////

    public bool IsValid =>
        TotalSizeNumber != null || FileSizeNumber != null || FileCount != null || CreatedAgeDate != null || ModifiedAgeDate != null;

    public string GetFullPath()
    {
        if (IsRelativePath && string.IsNullOrWhiteSpace(Host))
        {
            throw new CheckException($"could not find host for relative path '{Path}' of folder name '{Key}'. check if host group name exists");
        }

        return IsAbsolutePath || string.IsNullOrWhiteSpace(Host) ? Path : PathCombine(Host, Path);
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