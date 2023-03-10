using Planar.Common;
using Planar.Common.Exceptions;
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
    }
}