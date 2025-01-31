namespace Planar.Client.Entities
{
    public class MonitorEvent
    {
#if NETSTANDARD2_0
        public string EventName { get; set; }
        public string EventTitle { get; set; }
#else
        public string EventName { get; set; } = null!;
        public string EventTitle { get; set; } = null!;
#endif
    }
}