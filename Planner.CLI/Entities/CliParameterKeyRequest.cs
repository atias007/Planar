using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliParameterKeyRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Key { get; set; }
    }
}