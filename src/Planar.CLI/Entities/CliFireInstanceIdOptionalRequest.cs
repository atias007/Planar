using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliFireInstanceIdOptionalRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "fire instance id")]
        public string? FireInstanceId { get; set; }
    }
}