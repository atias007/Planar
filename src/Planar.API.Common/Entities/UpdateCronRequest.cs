namespace Planar.API.Common.Entities
{
    public class UpdateCronRequest : JobOrTriggerKey
    {
        public required string CronExpression { get; set; }
    }
}