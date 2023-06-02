using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetActiveJobsRequest : IQuietResult
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}