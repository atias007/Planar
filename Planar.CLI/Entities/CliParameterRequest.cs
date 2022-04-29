using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliParameterRequest : CliParameterKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Value { get; set; }
    }
}