using System;

namespace Planar.API.Common.Entities;

public class JobHistory
{
    public const int LogMaximumLength = 10_000;
    public const int DataMaximumLength = 4_000;

    public long Id { get; set; }

    public string InstanceId { get; set; } = null!;

    public string JobId { get; set; } = null!;

    public string JobName { get; set; } = null!;

    public string JobGroup { get; set; } = null!;

    public string JobType { get; set; } = null!;

    public string TriggerId { get; set; } = null!;

    public string TriggerName { get; set; } = null!;

    public string TriggerGroup { get; set; } = null!;

    public string? ServerName { get; set; }

    public int Status { get; set; }

    public string? StatusTitle { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [DisplayFormat(SpecialFormat = SpecialFormat.Duration)]
    public int? Duration { get; set; }

    [DisplayFormat(Format = "N0")]
    public int? EffectedRows { get; set; }

    [DisplayFormat(MaximumChars = DataMaximumLength + 250)]
    public string? Data { get; set; }

    [DisplayFormat(SpecialFormat = SpecialFormat.Log, MaximumChars = LogMaximumLength + 250)]
    public string? Log { get; set; }

    [DisplayFormat(MaximumChars = LogMaximumLength + 250)]
    public string? Exception { get; set; }

    public int ExceptionCount { get; set; }

    public bool Retry { get; set; }

    public bool IsCanceled { get; set; }

    public bool HasWarnings { get; set; }

    public byte? Anomaly { get; set; }

    public bool? IsOutlier => Anomaly == null ? null : Anomaly > 0;

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