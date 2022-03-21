namespace Planner.API.Common.Entities
{
    public class ApiMonitorAction
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int? EventArgument { get; set; }
        public string JobId { get; set; }
        public string Job { get; set; }
        public string JobGroup { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Hook { get; set; }
        public bool? Active { get; set; }
    }
}