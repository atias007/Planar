using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetRunningJobsRequest : CliFireInstanceIdOptionalRequest, IIterative, IQuietResult
    {
        [IterativeActionProperty]
        public bool Iterative { get; set; }

        [ActionProperty(ShortName = "d", LongName = "details")]
        public bool Details { get; set; }

        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}