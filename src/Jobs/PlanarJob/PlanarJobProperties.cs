using CommonJob;

namespace Planar;

public class PlanarJobProperties : BaseProcessJobProperties, IFileJobProperties
{
    public string Path { get; set; } = null!;
    public string Filename { get; set; } = null!;
}