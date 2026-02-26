using CommonJob;
using System.Collections.Generic;

namespace Planar;

public class PlanarJobProperties : BaseProcessJobProperties, IFileJobProperties
{
    public string Path { get; set; } = string.Empty;

    public string Filename { get; set; } = null!;

    public IEnumerable<string> Files =>
    [
        string.IsNullOrWhiteSpace(Path) ? Filename : System.IO.Path.Combine(Path, Filename)
    ];
}