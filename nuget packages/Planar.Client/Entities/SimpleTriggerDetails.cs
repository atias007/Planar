namespace Planar.Client.Entities
{
    public class SimpleTriggerDetails : TriggerDetails
    {
        public int RepeatCount { get; set; }

        public TimeSpan RepeatInterval { get; set; }

        public int TimesTriggered { get; set; }
    }
}