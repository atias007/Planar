using YamlDotNet.Serialization;

namespace Planar
{
    public sealed class SqlStep
    {
        [YamlMember(Alias = "connection name")]
        public string? ConnectionName { get; set; }

        public string? Name { get; set; }
        public string? Filename { get; set; }

        public TimeSpan? Timeout { get; set; }

        [YamlIgnore]
        public string FullFilename { get; set; } = null!;

        [YamlIgnore]
        public string Script { get; set; } = string.Empty;

        [YamlIgnore]
        internal string? ConnectionString { get; set; }
    }
}