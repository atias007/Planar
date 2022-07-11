using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConnectRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Host { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        public int Port { get; set; }

        [ActionProperty(DefaultOrder = 3)]
        public bool SSL { get; set; }
    }
}