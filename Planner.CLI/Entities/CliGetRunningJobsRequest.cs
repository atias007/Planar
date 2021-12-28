using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetRunningJobsRequest : CliFireInstanceIdRequest, IIterative
    {
        [IterativeActionProperty]
        public bool Iterative { get; set; }

        [ActionProperty(ShortName = "d", LongName = "details")]
        public bool Details { get; set; }
    }
}