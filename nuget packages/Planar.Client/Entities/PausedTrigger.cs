namespace Planar.Client.Entities
{
    public class PausedTrigger
    {
        public string Id { get; set; } = string.Empty;

        public string TriggerName { get; set; } = string.Empty;

        public string JobId { get; set; } = string.Empty;

        public string JobName { get; set; } = string.Empty;

        public string JobGroup { get; set; } = string.Empty;
    }
}