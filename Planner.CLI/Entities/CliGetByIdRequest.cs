using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetByIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public int Id { get; set; }
    }
}