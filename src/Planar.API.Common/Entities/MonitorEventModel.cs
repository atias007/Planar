namespace Planar.API.Common.Entities
{
    public class MonitorEventModel
    {
        public required int EventId { get; set; }
        public required string EventName { get; set; }
        public required string EventTitle { get; set; }
        public required string EventType { get; set; }
    }
}