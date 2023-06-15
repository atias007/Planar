using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum ConcurrentPeriod
    {
        Day, Week, Month, Year
    }

    public class CliGetConcurrentRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public ConcurrentPeriod? Period { get; set; }
    }
}