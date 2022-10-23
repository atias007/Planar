namespace Planar.API.Common.Entities
{
    public class AddMonitorRequest
    {
        public string Title { get; set; }

        public string JobId { get; set; }

        public string JobGroup { get; set; }

        public int MonitorEvent { get; set; }

        public string EventArguments { get; set; }

        public int GroupId { get; set; }

        public string Hook { get; set; }
    }
}