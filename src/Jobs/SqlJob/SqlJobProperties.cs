using CommonJob;
using System.Data;
using YamlDotNet.Serialization;

namespace Planar
{
    public class SqlJobProperties : IPathJobProperties
    {
        [YamlMember(Alias = "connection string")]
        public string? ConnectionString { get; set; }

        public string? Path { get; set; } = null!;

        public bool Transaction { get; set; }

        [YamlMember(Alias = "isolation level")]
        public IsolationLevel? IsolationLevel { get; set; }

        [YamlMember(Alias = "continue on error")]
        public bool ContinueOnError { get; set; }

        public List<SqlStep>? Steps { get; set; } = new List<SqlStep>();
    }
}