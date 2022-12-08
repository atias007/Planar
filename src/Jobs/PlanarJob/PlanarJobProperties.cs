using YamlDotNet.Serialization;

namespace Planar
{
    public class PlanarJobProperties
    {
        public string Path { get; set; }

        public string Filename { get; set; }

        [YamlMember(Alias = "class name")]
        public string ClassName { get; set; }
    }
}