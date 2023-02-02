using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetHistoryCountRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "hours")]
        [Required("hours argument is required")]
        public int Hours { get; set; }
    }
}