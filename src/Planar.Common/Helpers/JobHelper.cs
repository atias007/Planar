using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Quartz;

namespace Planar.Service.API.Helpers
{
    public static class JobHelper
    {
        public static string? GetJobAuthor(IJobDetail job)
        {
            if (job == null)
            {
                throw new PlanarException("job is null at JobHelper.GetJobAuthor(IJobDetail)");
            }

            if (job.JobDataMap.TryGetValue(Consts.Author, out var id))
            {
                return PlanarConvert.ToString(id);
            }

            return null;
        }

        public static string? GetJobId(IJobDetail? job)
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

        public static string GetKeyTitle(IJobDetail jobDetail)
        {
            var title = KeyHelper.GetKeyTitle(jobDetail.Key);
            return title;
        }
    }
}