using CommonJob;
using YamlDotNet.Serialization;

namespace Planar
{
    public class RestJobProperties : IPathJobProperties
    {
        public string? Path { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string Method { get; set; } = null!;

        [YamlMember(Alias = "body file")]
        public string? BodyFile { get; set; }

        [YamlMember(Alias = "ignore ssl errors")]
        public bool IgnoreSslErrors { get; set; }

        [YamlMember(Alias = "form data")]
        public Dictionary<string, string?> FormData { get; set; } = new();

        public Dictionary<string, string?> Headers { get; set; } = new();
    }
}