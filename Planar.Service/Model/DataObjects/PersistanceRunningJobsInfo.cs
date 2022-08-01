namespace Planar.Service.Model.DataObjects
{
    public class PersistanceRunningJobsInfo
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string InstanceId { get; set; }
        public string Information { get; set; }
        public string Exceptions { get; set; }

        public static PersistanceRunningJobsInfo Parse(PersistanceRunningJobInfo info)
        {
            var item = new PersistanceRunningJobsInfo();
            item.Exceptions = ReverseSafeString(info.Exceptions);
            item.Group = ReverseSafeString(info.Group);
            item.Information = ReverseSafeString(info.Information);
            item.InstanceId = ReverseSafeString(info.InstanceId);
            item.Name = ReverseSafeString(info.Name);
            return item;
        }

        private static string ReverseSafeString(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}