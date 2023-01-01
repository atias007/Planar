namespace Planar.API.Common.Entities
{
    public class UpdateMonitorRequest : AddMonitorRequest
    {
        public int Id { get; set; }

        public bool Active { get; set; }
    }
}