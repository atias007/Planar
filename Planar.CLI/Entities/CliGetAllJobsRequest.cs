using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAllJobsRequest : IQuietResult
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }

        [ActionProperty(ShortName = "a", LongName = "all")]
        public bool All { get; set; }

        [ActionProperty(ShortName = "s", LongName = "system")]
        public bool System { get; set; }

        [ActionProperty(ShortName = "su", LongName = "user")]
        public bool User { get; set; }
    }
}