using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob;

public abstract class BaseCommonJob(JobMonitorUtil jobMonitorUtil, ILogger logger) : IDisposable
{
    protected readonly string Seperator = string.Empty.PadLeft(40, '-');
    protected CancellationTokenSource? _executionTokenSource;
    private bool _disposed;
    private CancellationTokenSource? _durationTokenSource;
    private JobLogBroker _messageBroker = null!;
    protected JobFinishStatus FinishStatus { get; set; } = JobFinishStatus.Unknown;
    protected JobLogBroker MessageBroker => _messageBroker;
    protected IMonitorUtil MonitorUtil => jobMonitorUtil.MonitorUtil;
    protected IDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal void FillSettings(IDictionary<string, string?> settings)
    {
        Settings = settings;
    }

    internal void SetMessageBroker(JobLogBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    protected static void SafeInvoke(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch
        {
            DoNothingMethod();
        }
    }

    protected async static Task SafeInvoke(Func<Task> func)
    {
        try
        {
            await func.Invoke();
        }
        catch
        {
            DoNothingMethod();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _durationTokenSource?.Dispose();
                _executionTokenSource?.Dispose();
                _messageBroker?.Dispose();
            }
            _disposed = true;
        }
    }

    protected void HandleException(IJobExecutionContext context, Exception ex)
    {
        FinishStatus = JobFinishStatus.Failure;

        var metadata = JobExecutionMetadata.GetInstance(context);
        if (ex is TargetInvocationException)
        {
            metadata.UnhandleException = ex.InnerException;
        }
        else
        {
            metadata.UnhandleException = ex;
        }
    }

    protected void HandleSuccess()
    {
        FinishStatus = JobFinishStatus.Failure;
    }

    protected void SafeScan(MonitorEvents @event, IJobExecutionContext context)
    {
        try
        {
            MonitorUtil.Scan(@event, context);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeScan);
            logger.LogCritical(ex, "Error handle {Source}: {Message}", source, ex.Message);
        }
    }

    protected void StartMonitorDuration(IJobExecutionContext context)
    {
        var minutes = jobMonitorUtil.MonitorDurationCache.GetMonitorMinutes(context);
        if (!minutes.Any()) { return; }

        _durationTokenSource = new();

        foreach (var min in minutes)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(min * 60_000, _durationTokenSource.Token);
                    if (_durationTokenSource.IsCancellationRequested) { return; }
                    SafeScan(MonitorEvents.ExecutionDurationGreaterThanxMinutes, context);
                }
                catch (TaskCanceledException)
                {
                    // *** DO NOTHING ***
                }
            });
        }
    }

    protected void StopMonitorDuration()
    {
        _durationTokenSource?.Cancel();
        _durationTokenSource?.Dispose();
        _durationTokenSource = null;
    }

    protected async Task WaitForJobTask(IJobExecutionContext context, Task task)
    {
        try
        {
            await task.WaitAsync(_executionTokenSource?.Token ?? default);
            context.CancellationToken.ThrowIfCancellationRequested();
        }
        catch (TaskCanceledException)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                MessageBroker.AppendLog(LogLevel.Error, $"job canceled");
            }
            else
            {
                SafeScan(MonitorEvents.ExecutionTimeout, context);
                var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
                MessageBroker.AppendLog(LogLevel.Error, $"timeout occur, cancel the job running (timeout value: {FormatTimeSpan(timeout)})");
            }

            throw;
        }
    }

    private static void DoNothingMethod()
    {
        //// *** Do Nothing Method *** ////
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
        if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
        return $"{timeSpan:hh\\:mm\\:ss}";
    }
}

public abstract class BaseCommonJob<TProperties>(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : BaseCommonJob(jobMonitorUtil, logger), IJob
where TProperties : class, new()
{
    private const string errorMessage = "fail at {Source} with job {Group}.{Name}";

    protected readonly ILogger _logger = logger;
    public string FireInstanceId { get; private set; } = string.Empty;
    public TProperties Properties { get; private set; } = new();
    protected CancellationToken ExecutionCancellationToken => _executionTokenSource?.Token ?? default;

    public abstract Task Execute(IJobExecutionContext context);

    protected async Task FinalizeJob(IJobExecutionContext context)
    {
        try
        {
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            var source = nameof(FinalizeJob);
            _logger.LogError(ex, errorMessage, source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }

        JobExecutionMetadata? metadata = null;
        try
        {
            metadata = JobExecutionMetadata.GetInstance(context);
            metadata.Progress = 100;
        }
        catch (Exception ex)
        {
            var source = nameof(FinalizeJob);
            _logger.LogError(ex, errorMessage, source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }

        await SafeHandleSequence(context, metadata);
    }

    protected async Task Initialize(IJobExecutionContext context)
    {
        await SetProperties(context);

        string? path = null;
        if (Properties is IPathJobProperties pathProperties)
        {
            path = pathProperties.Path;
        }

        FillSettings(LoadJobSettings(path));
        SetMessageBroker(new JobLogBroker(context, Settings, MonitorUtil));

        context.CancellationToken.Register(() =>
        {
            MessageBroker.AppendLog(LogLevel.Error, "job get a request for cancel running");
        });

        _executionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
        _executionTokenSource.CancelAfter(timeout);

        SafeLogSequence(context);
    }

    protected IDictionary<string, string?> LoadJobSettings(string? path)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) { return new Dictionary<string, string?>(); }
            var jobSettings = JobSettingsLoader.LoadJobSettings(path, Global.GlobalConfig);
            return jobSettings;
        }
        catch (Exception ex)
        {
            var source = nameof(LoadJobSettings);
            _logger.LogError(ex, "fail at {Source}", source);
            throw new CommonJobException($"fail at {source}", ex);
        }
    }

    protected void ValidateMandatoryString(string? value, string propertyName)
    {
        if (!string.IsNullOrEmpty(value)) { value = value.Trim(); }
        if (string.IsNullOrEmpty(value))
        {
            throw new PlanarException($"property {propertyName} is mandatory for job '{GetType().FullName}'");
        }
    }

    protected private async Task SafeHandleSequence(IJobExecutionContext context, JobExecutionMetadata? metadata)
    {
        if (metadata == null) { return; }

        try
        {
            var sequenceInstanceId = JobHelper.GetSequenceInstanceId(context.MergedJobDataMap);
            var index = JobHelper.GetSequenceInstanceIndex(context.MergedJobDataMap);
            if (string.IsNullOrWhiteSpace(sequenceInstanceId)) { return; }
            var exception = metadata.UnhandleException;
            if (exception == null)
            {
                await SignalSequenceEvent(context, sequenceInstanceId, index, SequenceJobStepEvent.Success);
            }
            else
            {
                await SignalSequenceEvent(context, sequenceInstanceId, index, SequenceJobStepEvent.Fail);
            }
        }
        catch (Exception ex)
        {
            var source = nameof(SafeHandleSequence);
            _logger.LogError(ex, errorMessage, source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }
    }

    private async Task SignalSequenceEvent(IJobExecutionContext context, string sequenceInstanceId, int index, SequenceJobStepEvent @event)
    {
        var success = SequenceManager.SignalEvent(context.JobDetail.Key, context.FireInstanceId, sequenceInstanceId, index, @event);
        if (success) { return; }

        if (AppSettings.Cluster.Clustering)
        {
            await clusterUtil.SequenceSignalEvent(context.JobDetail.Key, context.FireInstanceId, sequenceInstanceId, index, (int)@event);
        }
    }

    private void SafeLogSequence(IJobExecutionContext context)
    {
        try
        {
            var instanceId = JobHelper.GetSequenceInstanceId(context.MergedJobDataMap);
            if (string.IsNullOrWhiteSpace(instanceId)) { return; }

            var triggerId = JobHelper.GetSequenceTriggerId(context.MergedJobDataMap);
            var jobKey = JobHelper.GetSequenceJobKey(context.MergedJobDataMap);
            MessageBroker.AppendLog(LogLevel.Information, Seperator);
            MessageBroker.AppendLog(LogLevel.Information, $"job was triggered by sequence");
            MessageBroker.AppendLog(LogLevel.Information, Seperator);
            MessageBroker.AppendLog(LogLevel.Information, $" key: {jobKey}");
            MessageBroker.AppendLog(LogLevel.Information, $" trigger: {triggerId}");
            MessageBroker.AppendLog(LogLevel.Information, $" fire instance id: {instanceId}");
            MessageBroker.AppendLog(LogLevel.Information, Seperator);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeLogSequence);
            _logger.LogError(ex, errorMessage, source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }
    }

    private async Task SetProperties(IJobExecutionContext context)
    {
        var jobId = JobHelper.GetJobId(context.JobDetail);
        if (jobId == null)
        {
            var title = JobHelper.GetKeyTitle(context.JobDetail);
            throw new PlanarException($"fail to get job id while execute job {title}");
        }

        var properties = await dataLayer.GetJobProperty(jobId);
        if (string.IsNullOrEmpty(properties))
        {
            var title = JobHelper.GetKeyTitle(context.JobDetail);
            throw new PlanarException($"fail to get job properties while execute job {title} (id: {jobId})");
        }

        Properties = YmlUtil.Deserialize<TProperties>(properties);
        FireInstanceId = context.FireInstanceId;
    }
}