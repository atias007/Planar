namespace Planar // **** DO NOT CHANGE NAMESPACE IDENTETION ****
{
    public enum MessageBrokerChannels
    {
        AddAggregateException,
        AppendLog,
        IncreaseEffectedRows,
        SetEffectedRows,
        PutJobData,
        PutTriggerData,
        RemoveJobData,
        RemoveTriggerData,
        ClearJobData,
        ClearTriggerData,
        UpdateProgress,
        ReportException,
        ReportExceptionV2,
        HealthCheck,
        MonitorCustomEvent,
        InvokeJob,
        QueueInvokeJob,
        FinishInvokeJob,
        AgentHealthCheck,
    }
}