namespace Planar.API.Common.Entities
{
    public class UpdateJobOptions
    {
        public bool UpdateJobData { get; set; }

        public bool UpdateTriggersData { get; set; }

        public static UpdateJobOptions Default => new() { UpdateJobData = false, UpdateTriggersData = false };
    }
}