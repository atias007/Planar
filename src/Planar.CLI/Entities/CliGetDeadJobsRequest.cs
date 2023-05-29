using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetDeadJobsRequest : IQuietResult
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}