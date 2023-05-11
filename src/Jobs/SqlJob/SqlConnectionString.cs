using YamlDotNet.Serialization;

namespace Planar
{
    public class SqlConnectionString
    {
        public string? Name { get; set; }

        [YamlMember(Alias = "connection string")]
        public string? ConnectionString { get; set; }
    }
}