using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliFireInstanceIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string FireInstanceId { get; set; }
    }
}