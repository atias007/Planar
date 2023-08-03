namespace Planar
{
    public enum MessageBrokerChannels
    {
        AddAggregateException,
        AppendLog,
        IncreaseEffectedRows,
        SetEffectedRows,
        PutJobData,
        PutTriggerData,
        UpdateProgress,
        ReportException
    }
}