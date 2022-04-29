using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliFireInstanceIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string FireInstanceId { get; set; }
    }
}