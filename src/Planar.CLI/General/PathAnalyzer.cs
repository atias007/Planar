using System.IO;

namespace Planar.CLI.General;

internal static class PathAnalyzer
{
    public record PathInfo(bool IsFolder, string Pattern, bool IsAbsolute, string Path)
    {
        public bool IsEmpty => string.IsNullOrWhiteSpace(Path);
        public bool IsLocal => (IsFolder && Directory.Exists(Path)) || (!IsFolder && File.Exists(Path));
    }

    public static PathInfo AnalyzePath(string input)
    {
        string pattern = "*.*";

        if (string.IsNullOrWhiteSpace(input)) { return new PathInfo(IsFolder: false, Pattern: pattern, IsAbsolute: false, Path: string.Empty); }
        var isAbsolute = Path.IsPathFullyQualified(input);

        // Extract pattern if wildcards are present
        var fileName = Path.GetFileName(input);
        if (!string.IsNullOrEmpty(fileName) && (fileName.Contains('*') || fileName.Contains('?')))
        {
            pattern = fileName;
            var path = Path.GetDirectoryName(input) ?? string.Empty;
            return new PathInfo(IsFolder: true, Pattern: pattern, IsAbsolute: isAbsolute, Path: path);
        }

        // Determine if it's a folder:
        // 1. Ends with a directory separator
        // 2. Has no file extension (and no wildcard pattern)
        // 3. Is a rooted path with no filename component
        var isFolder =
            input.EndsWith(Path.DirectorySeparatorChar) ||
            input.EndsWith(Path.AltDirectorySeparatorChar) ||
            (string.IsNullOrEmpty(Path.GetExtension(input)) && !string.IsNullOrEmpty(input));

        return new PathInfo(IsFolder: isFolder, Pattern: pattern, IsAbsolute: isAbsolute, Path: input);
    }
}