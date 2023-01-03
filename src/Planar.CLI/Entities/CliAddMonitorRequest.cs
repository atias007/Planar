using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddMonitorRequest
    {
        [ActionProperty("t", "title")]
        [Required]
        public string Title { get; set; }

        [ActionProperty("n", "name")]
        public string JobName { get; set; }

        [ActionProperty("jg", "job-group")]
        public string JobGroup { get; set; }

        [ActionProperty("e", "event")]
        [Required]
        public int EventId { get; set; }

        [ActionProperty("a", "arguments")]
        public string EventArgument { get; set; }

        [ActionProperty("g", "group")]
        [Required]
        public int GroupId { get; set; }

        [ActionProperty("h", "hook")]
        [Required]
        public string Hook { get; set; }
    }
}