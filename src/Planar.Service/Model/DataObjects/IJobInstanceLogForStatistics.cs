﻿namespace Planar.Service.Model.DataObjects
{
    internal interface IJobInstanceLogForStatistics
    {
        long Id { get; }
        string JobId { get; }
        int Status { get; }
        int? Duration { get; }
        int? EffectedRows { get; }
        bool IsCanceled { get; }
        byte? Anomaly { get; set; }
    }
}