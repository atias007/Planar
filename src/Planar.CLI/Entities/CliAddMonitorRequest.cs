using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddMonitorRequest
    {
        [ActionProperty("t", "title")]
        [Required("title argument is required")]
        public string Title { get; set; } = string.Empty;

        [ActionProperty("n", "name")]
        public string? JobName { get; set; }

        [ActionProperty("jg", "job-group")]
        public string? JobGroup { get; set; }

        [ActionProperty("e", "event")]
        [Required("event argument is required")]
        public int EventId { get; set; }

        [ActionProperty("a", "arguments")]
        public string? EventArgument { get; set; }

        [ActionProperty("g", "group")]
        [Required("group argument is required")]
        public int GroupId { get; set; }

        [ActionProperty("h", "hook")]
        [Required("hook argument is required")]
        public string Hook { get; set; } = string.Empty;
    }
}