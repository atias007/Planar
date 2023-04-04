using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [PersistJobDataAfterExecution]
    public class SqlJobConcurent : SqlJob
    {
        public SqlJobConcurent(ILogger<SqlJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}