namespace Planar.API.Common.Entities
{
    public class UpdateJobOptions
    {
        public bool UpdateJobDetails { get; set; }

        public bool UpdateJobData { get; set; }

        public bool UpdateProperties { get; set; }

        public bool UpdateTriggers { get; set; }

        public bool UpdateTriggersData { get; set; }

        public bool IsEmpty
        {
            get
            {
                var falseResult = UpdateJobDetails || UpdateJobData || UpdateProperties || UpdateTriggers || UpdateTriggersData;
                return !falseResult;
            }
        }
    }
}