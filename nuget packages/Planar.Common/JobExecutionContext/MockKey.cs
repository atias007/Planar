using Planar.Common;

namespace Planar.Job.Test.JobExecutionContext
{
    internal class MockKey : Key
    {
        public MockKey(string name, string group) : base(name, group)
        {
        }

        public MockKey(IExecuteJobProperties properties) : base(properties.TriggerKeyName, properties.TriggerKeyGroup)
        {
        }
    }
}