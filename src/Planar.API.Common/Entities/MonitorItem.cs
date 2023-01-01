namespace Planar.API.Common.Entities
{
    public class MonitorItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string EventTitle { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string Hook { get; set; }
        public bool Active { get; set; }
    }
}