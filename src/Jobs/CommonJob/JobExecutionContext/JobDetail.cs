// *** DO NOT EDIT NAMESPACE IDENTETION ***
namespace Planar.Job
{
    internal class JobDetail : IJobDetail
    {
#if NETSTANDARD2_0
        public IKey Key { get; set; }
        public string Id { get; set; }
#else
        public IKey Key { get; set; } = null!;
        public string Id { get; set; } = null!;
#endif

        public string Description { get; set; } = string.Empty;

        public IDataMap JobDataMap { get; set; } = new DataMap();

        public bool Durable { get; set; }

        public bool PersistJobDataAfterExecution { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public bool RequestsRecovery { get; set; }
    }
}