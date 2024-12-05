using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
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
    private JobLogBroker _messageBroker = null!;
    private CancellationTokenSource? _tokenSource;
    private bool _disposed;

    protected IDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();
    protected JobLogBroker MessageBroker => _messageBroker;
    protected IMonitorUtil MonitorUtil => jobMonitorUtil.MonitorUtil;

    protected static void DoNothingMethod()
    {
        //// *** Do Nothing Method *** ////
    }

    protected static void HandleException(IJobExecutionContext context, Exception ex)
    {
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

    protected async Task WaitForJobTask(IJobExecutionContext context, Task task)
    {
        var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
        try
        {
            await task.WaitAsync(timeout);
        }
        catch (TaskCanceledException)
        {
            SafeScan(MonitorEvents.ExecutionTimeout, context);
            MessageBroker.AppendLog(LogLevel.Warning, $"Timeout occur, sent cancel requst to job (timeout value: {FormatTimeSpan(timeout)})");
            await context.Scheduler.Interrupt(context.JobDetail.Key);
        }
    }

    protected void StartMonitorDuration(IJobExecutionContext context)
    {
        var minutes = jobMonitorUtil.MonitorDurationCache.GetMonitorMinutes(context);
        if (!minutes.Any()) { return; }

        _tokenSource = new();

        foreach (var min in minutes)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(min * 60_000, _tokenSource.Token);
                    if (_tokenSource.IsCancellationRequested) { return; }
                    SafeScan(MonitorEvents.ExecutionDurationGreaterThanxMinutes, context);
                }
                catch (TaskCanceledException)
                {
                    // *** DO NOTHING ***
                }
            });
        }
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

    protected void StopMonitorDuration()
    {
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = null;
    }

    internal void FillSettings(IDictionary<string, string?> settings)
    {
        Settings = settings;
    }

    internal void SetMessageBroker(JobLogBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
        if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
        return $"{timeSpan:hh\\:mm\\:ss}";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _tokenSource?.Dispose();
                _messageBroker?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public abstract class BaseCommonJob<TProperties>(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : BaseCommonJob(jobMonitorUtil, logger), IJob
    where TProperties : class, new()
{
    protected readonly ILogger _logger = logger;

    public TProperties Properties { get; private set; } = new();

    public string FireInstanceId { get; private set; } = string.Empty;

    public abstract Task Execute(IJobExecutionContext context);

    protected void FinalizeJob(IJobExecutionContext context)
    {
        try
        {
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            var source = nameof(FinalizeJob);
            _logger.LogError(ex, "fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }

        try
        {
            var metadata = JobExecutionMetadata.GetInstance(context);
            metadata.Progress = 100;
        }
        catch (Exception ex)
        {
            var source = nameof(FinalizeJob);
            _logger.LogError(ex, "fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
        }
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
            MessageBroker.AppendLog(LogLevel.Warning, "Service get a request for cancel job");
        });
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