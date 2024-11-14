using Planar.Common;
using Quartz;
using System;

namespace CommonJob;

public class InterceptMessageEventArgs(MonitorEvents @event, IJobExecutionContext context, Exception? exception = null) : EventArgs
{
    public MonitorEvents MonitorEvent { get; } = @event;
    public IJobExecutionContext ExecutionContext { get; } = context;
    public Exception? Exception { get; } = exception;
}

public static class PlanarBrokerService
{
    public static event EventHandler<InterceptMessageEventArgs>? InterceptingMessage;

    public static void OnInterceptingMessage(MonitorEvents @event, IJobExecutionContext context, Exception? exception = null)
    {
        var e = new InterceptMessageEventArgs(@event, context, exception);
        InterceptingMessage?.Invoke(null, e);
    }
}