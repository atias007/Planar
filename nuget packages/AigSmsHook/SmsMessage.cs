namespace Aig.Planar.SmsHook
{
    internal class SmsMessage
    {
        public required string MessageText { get; set; }
        public required string ToPhone { get; set; }
        public required string SourceSystem { get; set; }
        public bool OverrideWorkingHours { get; set; }
    }
}