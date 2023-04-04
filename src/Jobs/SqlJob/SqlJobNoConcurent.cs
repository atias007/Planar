using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class SqlJobNoConcurent : SqlJob
    {
        public SqlJobNoConcurent(ILogger<SqlJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}