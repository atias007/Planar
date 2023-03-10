using Planar.Common.Exceptions;
using Quartz;

namespace Planar.Common.API.Helpers
{
    public static class JobIdHelper
    {
        public static string? GetJobId(IJobDetail job)
        {
            if (job == null)
            {
                throw new PlanarException("job is null at JobKeyHelper.GetJobId(IJobDetail)");
            }

            if (job.JobDataMap.TryGetValue(Consts.JobId, out var id))
            {
                return PlanarConvert.ToString(id);
            }

            return null;
        }
    }
}