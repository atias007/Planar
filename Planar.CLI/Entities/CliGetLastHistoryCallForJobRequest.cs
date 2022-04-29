using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest
    {
        [ActionProperty(Default = true)]
        public int LastDays { get; set; }
    }
}