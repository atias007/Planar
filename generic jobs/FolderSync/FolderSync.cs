using Common;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace FolderSync;

internal class FolderSync : BaseOperation, INamedCheckElement, IVetoEntity
{
    public FolderSync(IConfigurationSection section, BaseDefault @default) : base(section, @default)
    {
        Name = section.GetValue<string>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        SourcePath = section.GetValue<string>("source path") ?? string.Empty;
        TargetPath = section.GetValue<string>("target path") ?? string.Empty;
        ExcludeHidden = section.GetValue<bool>("exclude hidden");
        FailJobWithErrors = section.GetValue<bool>("fail job with errors");
        StopAtFirstError = section.GetValue<bool>("stop at first error");
        LogIoInformation = section.GetValue<bool>("log io information");
        DeleteFromTarget = section.GetValue<bool>("delete from target");
        ExcludeSourceFiles = section.GetValue<IEnumerable<string>?>("exclude source files");
        ExcludeSourceDirectories = section.GetValue<IEnumerable<string>?>("exclude source directories");
        IncludeSourceFiles = section.GetValue<IEnumerable<string>?>("include source files");
        IncludeSourceDirectories = section.GetValue<IEnumerable<string>?>("include source directories");
        ExcludeDeleteTargetFiles = section.GetValue<IEnumerable<string>?>("exclude delete target files");
        ExcludeDeleteTargetDirectories = section.GetValue<IEnumerable<string>?>("exclude delete target directories");
    }

    public FolderSync(FolderSync folder) : base(folder)
    {
        Name = folder.Name;
        HostGroupName = folder.HostGroupName;
        SourcePath = folder.SourcePath;
        TargetPath = folder.TargetPath;
        ExcludeHidden = folder.ExcludeHidden;
        FailJobWithErrors = folder.FailJobWithErrors;
        StopAtFirstError = folder.StopAtFirstError;
        LogIoInformation = folder.LogIoInformation;
        DeleteFromTarget = folder.DeleteFromTarget;
        ExcludeSourceFiles = folder.ExcludeSourceFiles;
        ExcludeSourceDirectories = folder.ExcludeSourceDirectories;
        IncludeSourceFiles = folder.IncludeSourceFiles;
        IncludeSourceDirectories = folder.IncludeSourceDirectories;
        ExcludeDeleteTargetFiles = folder.ExcludeDeleteTargetFiles;
        ExcludeDeleteTargetDirectories = folder.ExcludeDeleteTargetDirectories;
        IsAbsolutePath = folder.IsAbsolutePath;
    }

    public string Name { get; set; }
    public string? HostGroupName { get; private set; }
    public string SourcePath { get; private set; }
    public string TargetPath { get; private set; }
    public bool ExcludeHidden { get; private set; }
    public bool DeleteFromTarget { get; private set; }
    public bool FailJobWithErrors { get; private set; }
    public bool StopAtFirstError { get; private set; }
    public bool LogIoInformation { get; private set; }
    public IEnumerable<string>? ExcludeSourceFiles { get; private set; }
    public IEnumerable<string>? ExcludeSourceDirectories { get; private set; }
    public IEnumerable<string>? IncludeSourceFiles { get; private set; }
    public IEnumerable<string>? IncludeSourceDirectories { get; private set; }
    public IEnumerable<string>? ExcludeDeleteTargetFiles { get; private set; }
    public IEnumerable<string>? ExcludeDeleteTargetDirectories { get; private set; }

    //// --------------------------------------- ////

    public bool IsAbsolutePath { get; private set; }

    public string Key => Name;
    public bool IsRelativePath => !IsAbsolutePath;

    // internal use for relative urls
    public string? Host { get; set; }

    public string GetFullSourcePath()
    {
        return IsAbsolutePath || string.IsNullOrWhiteSpace(Host) ? SourcePath : PathCombine(Host, SourcePath);
    }

    public string GetFullTargetPath()
    {
        return IsAbsolutePath || string.IsNullOrWhiteSpace(Host) ? TargetPath : PathCombine(Host, TargetPath);
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