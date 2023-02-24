using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliCronExpression
    {
        [Required("expression argument is required")]
        [ActionProperty(DefaultOrder = 0)]
        public string Expression { get; set; } = string.Empty;
    }
}