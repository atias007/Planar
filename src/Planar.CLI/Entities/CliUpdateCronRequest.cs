using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateCronRequest : CliTriggerKey
    {
        [Required("cron-expression argument is required")]
        [ActionProperty(DefaultOrder = 1)]
        public string CronExpression { get; set; } = string.Empty;
    }
}