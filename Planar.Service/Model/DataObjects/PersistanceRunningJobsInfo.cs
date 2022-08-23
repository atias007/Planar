namespace Planar.Service.Model.DataObjects
{
    public class PersistanceRunningJobsInfo
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string InstanceId { get; set; }
        public string Log { get; set; }
        public string Exceptions { get; set; }
        public int Duration { get; set; }

        public static PersistanceRunningJobsInfo Parse(PersistanceRunningJobInfo info)
        {
            var item = new PersistanceRunningJobsInfo
            {
                Exceptions = ReverseSafeString(info.Exceptions),
                Group = ReverseSafeString(info.Group),
                Log = ReverseSafeString(info.Log),
                InstanceId = ReverseSafeString(info.InstanceId),
                Name = ReverseSafeString(info.Name),
                Duration = info.Duration
            };
            return item;
        }

        private static string ReverseSafeString(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}