using CommonJob;
using YamlDotNet.Serialization;

namespace Planar
{
    public class PlanarJobProperties : IFileJobProperties
    {
        public string? Path { get; set; }

        public string? Filename { get; set; }

        [YamlMember(Alias = "class name")]
        public string? ClassName { get; set; }
    }
}