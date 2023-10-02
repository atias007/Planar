namespace Planar.Client.Entities
{
    public class CronTriggerDetails : TriggerDetails
    {
        public string CronExpression { get; set; } = string.Empty;

        public string CronDescription { get; set; } = string.Empty;
    }
}