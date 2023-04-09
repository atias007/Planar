using CommonJob;
using System.Data;
using YamlDotNet.Serialization;

namespace Planar
{
    public class SqlJobProperties : IPathJobProperties
    {
        public string? Path { get; set; } = null!;

        [YamlMember(Alias = "default connection name")]
        public string? DefaultConnectionName { get; set; }

        public bool Transaction { get; set; }

        [YamlMember(Alias = "transaction isolation level")]
        public IsolationLevel? TransactionIsolationLevel { get; set; }

        [YamlMember(Alias = "continue on error")]
        public bool ContinueOnError { get; set; }

        public List<SqlStep>? Steps { get; set; } = new List<SqlStep>();

        [YamlMember(Alias = "connection strings")]
        public List<SqlConnectionString>? ConnectionStrings { get; set; }

        [YamlIgnore]
        internal string? DefaultConnectionString { get; set; }
    }
}