using System.Collections.Generic;

namespace Planar.Service.Model.DataObjects
{
    internal class JobStatistics
    {
        public IEnumerable<JobDurationStatistic> JobDurationStatistics { get; init; } = null!;

        public IEnumerable<JobEffectedRowsStatistic> JobEffectedRowsStatistic { get; init; } = null!;
    }
}