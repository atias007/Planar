using System;

namespace Planar.Client.Entities
{
    public class HistoryDetails
    {
        public long Id { get; set; }

#if NETSTANDARD2_0
        public string InstanceId { get; set; }

        public string JobId { get; set; }

        public string JobName { get; set; }

        public string JobGroup { get; set; }

        public string JobType { get; set; }

        public string TriggerId { get; set; }

        public string TriggerName { get; set; }

        public string TriggerGroup { get; set; }

        public string ServerName { get; set; }

        public string StatusTitle { get; set; }

        public string Data { get; set; }

        public string Log { get; set; }

        public string Exception { get; set; }

#else
        public string InstanceId { get; set; } = null!;

        public string JobId { get; set; } = null!;

        public string JobName { get; set; } = null!;

        public string JobGroup { get; set; } = null!;

        public string JobType { get; set; } = null!;

        public string TriggerId { get; set; } = null!;

        public string TriggerName { get; set; } = null!;

        public string TriggerGroup { get; set; } = null!;

        public string? ServerName { get; set; }

        public string? StatusTitle { get; set; }
        public string? Data { get; set; }

        public string? Log { get; set; }

        public string? Exception { get; set; }

#endif

        public int Status { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? Duration { get; set; }

        public int? EffectedRows { get; set; }

        public int ExceptionCount { get; set; }

        public bool Retry { get; set; }

        public bool IsCanceled { get; set; }

        public byte? Anomaly { get; set; }

        public bool? IsOutlier => Anomaly == null ? (bool?)null : Anomaly > 0;

        public bool HasWarnings { get; set; }

        public string AnomalyTitle
        {
            get
            {
                AnomalyMembers result = AnomalyMembers.Undefined;
                if (Anomaly != null)
                {
                    try
                    {
                        result = (AnomalyMembers)Anomaly.GetValueOrDefault();
                    }
                    catch
                    {
                        // *** DO NOTHING *** //
                    }
                }

                return result.ToString();
            }
        }

        private enum AnomalyMembers : byte
        {
            Undefined = 255,
            Normalcy = 0,
            DurationAnomaly = 1,
            EffectedRowsAnomaly = 2,
            StoppedJob = 100,
            StatusFail = 101,
        }
    }
}