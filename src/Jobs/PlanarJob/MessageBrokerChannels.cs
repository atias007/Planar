namespace Planar
{
    internal enum MessageBrokerChannels
    {
        AddAggregateException,
        AppendLog,
        IncreaseEffectedRows,
        SetEffectedRows,
        PutJobData,
        PutTriggerData,
        UpdateProgress,
        ReportException,
        ReportExceptionV2,
        HealthCheck
    }
}