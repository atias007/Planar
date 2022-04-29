using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliParameterKeyRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Key { get; set; }
    }
}