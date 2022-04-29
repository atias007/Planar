using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAllJobsRequest : IQuietResult
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}