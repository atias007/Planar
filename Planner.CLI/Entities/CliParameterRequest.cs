using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliParameterRequest : CliParameterKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Value { get; set; }
    }
}