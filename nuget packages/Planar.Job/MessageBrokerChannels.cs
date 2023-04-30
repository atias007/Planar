namespace Planar
{
    internal enum MessageBrokerChannels
    {
        AddAggregateException,
        AppendLog,
        FailOnStopRequest,
        GetExceptionsText,
        GetData,
        CheckIfStopRequest,
        GetEffectedRows,
        IncreaseEffectedRows,
        SetEffectedRows,
        DataContainsKey,
        PutJobData,
        PutTriggerData,
        UpdateProgress,
        JobRunTime
    }
}