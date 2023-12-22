namespace Planar.Job
{
    internal class JobDetail : IJobDetail
    {
        public IKey Key { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public IDataMap JobDataMap { get; set; } = new DataMap();

        public bool Durable { get; set; }

        public bool PersistJobDataAfterExecution { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public bool RequestsRecovery { get; set; }

        public string Id { get; set; } = null!;
    }
}