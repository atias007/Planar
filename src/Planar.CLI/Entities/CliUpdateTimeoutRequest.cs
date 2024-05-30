using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateTimeoutRequest : CliTriggerKey
    {
        [Required("timeout argument is required")]
        [ActionProperty(DefaultOrder = 1)]
        public string Timeout { get; set; } = string.Empty;
    }
}