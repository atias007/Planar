using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.General
{
    internal static class StatisticsUtil
    {
        public static TimeSpan DefaultCacheSpan => TimeSpan.FromMinutes(65);

        public static void SetAnomaly(IJobInstanceLogForStatistics item, JobStatistics statistics)
        {
            if (item.Status == -1) { return; } // Still running, skip anomaly check
            if (item.Status == 1) // Fail Job
            {
                item.Anomaly = 101;
                return;
            }

            if (item.IsCanceled) // Stopped Job
            {
                item.Anomaly = 100;
                return;
            }

            var durationAnomaly = IsDurationAnomaly(item, statistics.JobDurationStatistics);
            if (durationAnomaly)
            {
                item.Anomaly = 1; // Duration anomaly
                return;
            }

            var effectedRowsAnomaly = IsEffectedRowsAnomaly(item, statistics.JobEffectedRowsStatistic);
            if (effectedRowsAnomaly)
            {
                item.Anomaly = 2; // Effected rows anomaly
                return;
            }

            item.Anomaly = 0; // No anomaly
        }

        private static decimal GetZScore(decimal value, decimal avg, decimal stdev)
        {
            if (stdev == 0) { return 0; }
            return (value - avg) / stdev;
        }

        private static decimal GetZScore(int value, decimal avg, decimal stdev)
        {
            var decValue = Convert.ToDecimal(value);
            return GetZScore(decValue, avg, stdev);
        }

        private static bool IsOutlier(decimal zscore, decimal lowerBound = -1.96M, decimal upperBound = 1.96M)
        {
            return zscore > upperBound || zscore < lowerBound;
        }

        private static bool IsDurationAnomaly(IJobInstanceLogForStatistics? log, IEnumerable<JobDurationStatistic> statistics)
        {
            if (log == null) { return false; }

            var stat = statistics.FirstOrDefault(j => j.JobId == log.JobId);
            if (stat == null) { return false; }

            var duration = log.Duration.GetValueOrDefault();
            var durationScore = GetZScore(duration, stat.AvgDuration, stat.StdevDuration);
            var durationAnomaly = IsOutlier(durationScore);
            return durationAnomaly;
        }

        private static bool IsEffectedRowsAnomaly(IJobInstanceLogForStatistics? log, IEnumerable<JobEffectedRowsStatistic> statistics)
        {
            if (log == null) { return false; }

            var stat = statistics.FirstOrDefault(j => j.JobId == log.JobId);
            if (stat == null) { return false; }
            if (stat.StdevEffectedRows == 0) { return false; }

            var effectedRows = log.EffectedRows.GetValueOrDefault();
            var effectedRowsScore = GetZScore(effectedRows, stat.AvgEffectedRows, stat.StdevEffectedRows);
            var durationAnomaly = IsOutlier(effectedRowsScore);
            return durationAnomaly;
        }
    }
}