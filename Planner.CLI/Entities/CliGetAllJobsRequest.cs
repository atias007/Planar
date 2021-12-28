using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetAllJobsRequest : IQuietResult
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}