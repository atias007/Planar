// *** DO NOT EDIT NAMESPACE IDENTETION ***
namespace Planar.Job
{
    public interface IJobDetail
    {
        bool ConcurrentExecutionDisallowed { get; }
        string Description { get; }
        bool Durable { get; }
        IDataMap JobDataMap { get; }
        IKey Key { get; }
        string Id { get; }
        bool PersistJobDataAfterExecution { get; }
        bool RequestsRecovery { get; }
    }
}