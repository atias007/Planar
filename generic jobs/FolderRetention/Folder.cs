using Common;
using Microsoft.Extensions.Configuration;

namespace FolderRetention;

internal class Folder : INamedCheckElement
{
    public Folder(IConfigurationSection section)
    {
        Name = section.GetValue<string>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        Path = section.GetValue<string>("path") ?? string.Empty;
        FilesPattern = section.GetValue<string?>("files pattern")?.Split(',').ToList();
        IncludeSubdirectories = section.GetValue<bool>("include subdirectories");
        DeleteEmptyDirectories = section.GetValue<bool>("delete empty directories");
        FileSize = section.GetValue<string?>("file size");
        CreatedAge = section.GetValue<string?>("created age");
        ModifiedAge = section.GetValue<string?>("modified age");
        MaxFiles = section.GetValue<int>("max files");
        Active = section.GetValue<bool?>("active") ?? true;

        FileSizeNumber = CommonUtil.GetSize(FileSize, "file size");
        CreatedAgeDate = CommonUtil.GetDateFromSpan(CreatedAge, "created age");
        ModifiedAgeDate = CommonUtil.GetDateFromSpan(ModifiedAge, "modified age");
        IsAbsolutePath = System.IO.Path.IsPathFullyQualified(Path);

        if (FilesPattern == null || !FilesPattern.Any())
        {
            FilesPattern = new List<string> { "*.*" };
        }
    }

    public Folder(Folder folder)
    {
        Name = folder.Name;
        HostGroupName = folder.HostGroupName;
        Path = folder.Path;
        FilesPattern = folder.FilesPattern;
        IncludeSubdirectories = folder.IncludeSubdirectories;
        DeleteEmptyDirectories = folder.DeleteEmptyDirectories;
        FileSize = folder.FileSize;
        CreatedAge = folder.CreatedAge;
        ModifiedAge = folder.ModifiedAge;
        MaxFiles = folder.MaxFiles;
        Active = folder.Active;

        FileSizeNumber = folder.FileSizeNumber;
        CreatedAgeDate = folder.CreatedAgeDate;
        ModifiedAgeDate = folder.ModifiedAgeDate;
        IsAbsolutePath = folder.IsAbsolutePath;
    }

    public string Name { get; set; }
    public string? HostGroupName { get; private set; }
    public string Path { get; private set; }
    public IEnumerable<string>? FilesPattern { get; private set; }
    public bool IncludeSubdirectories { get; private set; }
    public bool DeleteEmptyDirectories { get; private set; }
    public string? FileSize { get; private set; }
    public string? CreatedAge { get; private set; }
    public string? ModifiedAge { get; private set; }
    public int MaxFiles { get; private set; }
    public bool Active { get; private set; }

    //// --------------------------------------- ////

    public long? FileSizeNumber { get; private set; }
    public DateTime? CreatedAgeDate { get; private set; }
    public DateTime? ModifiedAgeDate { get; private set; }
    public bool IsAbsolutePath { get; private set; }

    public string Key => Name;
    public bool IsRelativePath => !IsAbsolutePath;

    public TimeSpan? Span => null;

    // internal use for relative urls
    public string? Host { get; set; }

    public bool IsValid()
    {
        return FileSizeNumber != null || CreatedAgeDate != null || ModifiedAgeDate != null || DeleteEmptyDirectories;
    }

    public string GetFullPath()
    {
        return IsAbsolutePath || string.IsNullOrWhiteSpace(Host) ? Path : PathCombine(Host, Path);
    }

    private static string PathCombine(string part1, string part2)
    {
        part1 = part1.Trim().TrimEnd(System.IO.Path.DirectorySeparatorChar);
        part2 = part2.Trim().TrimStart(System.IO.Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(part1)) { return part2; }
        if (string.IsNullOrWhiteSpace(part2)) { return part1; }
        return $"{part1}{System.IO.Path.DirectorySeparatorChar}{part2}";
    }
}