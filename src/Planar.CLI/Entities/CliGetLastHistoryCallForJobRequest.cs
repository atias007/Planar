using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest
    {
        [ActionProperty(Default = true, Name = "last days")]
        public int LastDays { get; set; }
    }
}