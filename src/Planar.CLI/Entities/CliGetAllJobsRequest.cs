using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAllJobsRequest : IQuietResult
    {
        [ActionProperty(DefaultOrder = 0, Name = "group")]
        public string? JobGroup { get; set; }

        [QuietActionProperty]
        public bool Quiet { get; set; }

        [ActionProperty("a", "all")]
        public bool All { get; set; }

        [ActionProperty("s", "system")]
        public bool System { get; set; }

        [ActionProperty("t", "type")]
        public string? JobType { get; set; }
    }
}