namespace Planar.Service.Model.DataObjects
{
    public class PersistanceRunningJobsInfo
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string InstanceId { get; set; }
        public string Information { get; set; }
        public string Exceptions { get; set; }
    }
}