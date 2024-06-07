using Common;
using Microsoft.Extensions.Configuration;

namespace FolderRetention;

internal class Folder(IConfigurationSection section) : INamedCheckElement
{
    public string Name { get; set; } = section.GetValue<string>("name") ?? string.Empty;
    public string Path { get; private set; } = section.GetValue<string>("path") ?? string.Empty;
    public IEnumerable<string>? FilesPattern { get; private set; } = section.GetValue<string?>("files pattern")?.Split(',').ToList();
    public bool IncludeSubdirectories { get; private set; } = section.GetValue<bool>("include subdirectories");
    public bool DeleteEmptyDirectories { get; private set; } = section.GetValue<bool>("delete empty directories");
    public string? FileSize { get; private set; } = section.GetValue<string?>("file size");
    public string? CreatedAge { get; private set; } = section.GetValue<string?>("created age");
    public string? ModifiedAge { get; private set; } = section.GetValue<string?>("modified age");
    public int MaxFiles { get; private set; } = section.GetValue<int>("max files");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    //// --------------------------------------- ////

    public long? FileSizeNumber { get; private set; }
    public DateTime? CreatedAgeDate { get; private set; }
    public DateTime? ModifiedAgeDate { get; private set; }
    public string Key => Name;
    public bool IsAbsolutePath { get; set; }

    public TimeSpan? Span => null;

    public void SetDefaultFilePattern()
    {
        FilesPattern = new List<string> { "*.*" };
    }

    public bool IsValid()
    {
        return FileSizeNumber != null || CreatedAgeDate != null || ModifiedAgeDate != null || DeleteEmptyDirectories;
    }

    public string GetFullPath(string? host)
    {
        return IsAbsolutePath || string.IsNullOrWhiteSpace(host) ? Path : PathCombine(host, Path);
    }

    //// --------------------------------------- ////
    public void SetFolderArguments()
    {
        FileSizeNumber = CommonUtil.GetSize(FileSize, "file size");
        CreatedAgeDate = CommonUtil.GetDateFromSpan(CreatedAge, "created age");
        ModifiedAgeDate = CommonUtil.GetDateFromSpan(ModifiedAge, "modified age");
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