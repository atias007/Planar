using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class SqlJobNoConcurrent : SqlJob
    {
        public SqlJobNoConcurrent(ILogger<SqlJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}