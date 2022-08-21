using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetInfoRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Key { get; set; }
    }
}