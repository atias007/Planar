using CommonJob;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Planar;

public abstract class BaseProcessJobProperties : IFileJobProperties
{
    protected BaseProcessJobProperties()
    {
        Filename = string.Empty;
    }

    [YamlIgnore]
    public string Path { get; private set; } = null!;

    [YamlMember(Alias = "filename", Order = 0)]
    public string Filename
#pragma warning restore CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.
    {
        get;
        set
        {
            field = value;
            if (string.IsNullOrWhiteSpace(value))
            {
                Path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
            }
            else
            {
                var fullname =
                    System.IO.Path.IsPathFullyQualified(value) ?
                    value :
                    FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, value);

                var fi = new FileInfo(fullname);
                Path = fi.DirectoryName ?? string.Empty;
            }
        }
    }

    [YamlMember(Alias = "domain", Order = 1)]
    public string? Domain { get; set; }

    [YamlMember(Alias = "password", Order = 2)]
    public string? Password { get; set; }

    [YamlMember(Alias = "username", Order = 3)]
    public string? UserName { get; set; }

    [YamlIgnore]
    public IEnumerable<string> Files => string.IsNullOrWhiteSpace(Filename) ? [] : [Filename];
}