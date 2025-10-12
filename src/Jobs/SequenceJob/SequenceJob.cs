using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.General;
using Quartz;

namespace Planar;

public abstract class SequenceJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IValidator<SequenceJobProperties> validator) :
    BaseCommonJob<SequenceJobProperties>(logger, dataLayer, jobMonitorUtil, clusterUtil),
    ISequenceInstance
{
    private ResetEventWrapper? _resetEvent;
    private readonly List<(bool, string)> _logs = [];

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            await ValidateSequenceJob(ExecutionCancellationToken);
            _ = SafeStartMonitorDuration(context);
            SequenceManager.RegisterSequence(context.FireInstanceId, this);
            var task = ExecuteSequence(context);
            await WaitForJobTask(context, task);
            StopMonitorDuration();
            HandleSuccess();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            SafeInvoke(LogSequenceSummary);
            SafeInvoke(() => SequenceManager.UnregisterSequence(context.FireInstanceId));
            await FinalizeJob(context);
            SafeInvoke(() => { _resetEvent?.Dispose(); });
            SafeInvoke(() => { _resetEvent = null; });
        }
    }

    public bool SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event)
    {
        if (_resetEvent == null) { return false; }

        var key = ResetEventWrapper.GetKey(stepJobKey, sequenceFireInstanceId);
        if (_resetEvent.Key != key) { return false; }
        _resetEvent.Event = @event;
        _resetEvent.FireInstanceId = fireInstanceId;
        _resetEvent.ResetEvent.Set();
        return true;
    }

    private void LogSequenceSummary()
    {
        void AppendInfoLog(string message)
        {
            MessageBroker.AppendLog(LogLevel.Information, message);
        }

        void AppendWarnLog(string message)
        {
            MessageBroker.AppendLog(LogLevel.Warning, message);
        }

        if (_logs.Count == 0) { return; }

        AppendInfoLog("  ");
        AppendInfoLog(Seperator);
        AppendInfoLog(" sequence steps summary");
        AppendInfoLog(Seperator);
        AppendInfoLog("[job key] --> [fire instance id] --> [status]");
        AppendInfoLog(string.Empty);
        foreach (var log in _logs)
        {
            if (log.Item1)
            {
                AppendWarnLog(log.Item2);
            }
            else
            {
                AppendInfoLog(log.Item2);
            }
        }

        AppendInfoLog(Seperator);
    }

    private static JobDataMap GetJobDataMap(IJobExecutionContext context, SequenceJobStep step)
    {
        var data = context.MergedJobDataMap;
        foreach (var item in step.Data)
        {
            var value = item.Value ?? string.Empty;
            if (data.ContainsKey(item.Key))
            {
                data[item.Key] = value;
            }
            else
            {
                data.Add(item.Key, value);
            }
        }

        var triggerId = TriggerHelper.GetTriggerId(context.Trigger);
        if (string.IsNullOrWhiteSpace(triggerId))
        {
            triggerId = Consts.ManualTriggerId;
        }
        else
        {
            triggerId = context.Trigger.Key.Name;
        }

        data.TryAdd(Consts.SequenceInstanceIdDataKey, context.FireInstanceId);
        data.TryAdd(Consts.SequenceJobKeyDataKey, context.JobDetail.Key.ToString());
        data.TryAdd(Consts.SequenceTriggerIdDataKey, triggerId);

        return data;
    }

    private static JobKey ParseJobKey(string key)
    {
        var parts = key.Split('.');
        if (parts.Length == 1)
        {
            return new JobKey(parts[0]);
        }
        else if (parts.Length == 2)
        {
            return new JobKey(parts[1], parts[0]);
        }
        else
        {
            throw new ArgumentException($"invalid job key: {key}");
        }
    }

    private async Task ExecuteSteps(IJobExecutionContext context, IEnumerable<SequenceJobStep> steps, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException(); }

        var totalSteps = steps.Count();

        // run steps
        foreach (var (index, step) in steps.Index())
        {
            // prepare
            var jobKey = ParseJobKey(step.Key);
            var data = GetJobDataMap(context, step);
            _resetEvent = ResetEventWrapper.Create(jobKey, context.FireInstanceId, data, step);
            var timeout = _resetEvent.Timeout ?? AppSettings.General.JobAutoStopSpan;
            if (_resetEvent.Status != StepStatus.Waiting) { return; }
            MessageBroker.AppendLog(LogLevel.Information, $" - [step {index + 1}] start job: {jobKey}");

            // execute step
            _resetEvent.SetStatus(StepStatus.Start);
            await context.Scheduler.TriggerJob(jobKey, data, cancellationToken);
            var result = _resetEvent.ResetEvent.WaitOne(timeout);

            // handle step timeout | check for cancellation token
            if (cancellationToken.IsCancellationRequested)
            {
                _resetEvent.SetStatus(StepStatus.Interrupted);
                AddLog(_resetEvent);
                throw new TaskCanceledException();
            }

            if (!result)
            {
                _resetEvent.SetStatus(StepStatus.Interrupted);
                AddLog(_resetEvent);
                throw new TimeoutException($"step {index + 1} timeout");
            }

            // check for fail step
            if (_resetEvent.Event == SequenceJobStepEvent.Fail)
            {
                _resetEvent.SetStatus(StepStatus.Interrupted);
                AddLog(_resetEvent);
                if (Properties.StopRunningOnFail)
                {
                    throw new OperationCanceledException($"step {index + 1} fail and 'stop running on fail' is true");
                }
            }
            else
            {
                // update status
                _resetEvent.SetStatus(StepStatus.Finish);
                AddLog(_resetEvent);
                MessageBroker.IncreaseEffectedRows(1);
                MessageBroker.UpdateProgress(index + 1, totalSteps);
            }
        }
    }

    private async Task ExecuteSequence(IJobExecutionContext context)
    {
        // register cancellation token event
        ExecutionCancellationToken.Register(() =>
        {
            _resetEvent?.ResetEvent.Set();
        });

        // find startup step
        var steps = Properties.Steps;
        MessageBroker.AppendLog(LogLevel.Information, $"start sequence with {steps.Count} steps");

        // start sequence
        await ExecuteSteps(context, Properties.Steps, ExecutionCancellationToken);

        // handle cancel steps
        ExecutionCancellationToken.ThrowIfCancellationRequested();
    }

    private void AddLog(ResetEventWrapper wrapper)
    {
        var isWarning = wrapper.Event == SequenceJobStepEvent.Fail;
        var log = $"{wrapper.JobKey} --> {wrapper.FireInstanceId} --> {wrapper.DisplayStatus}";
        _logs.Add((isWarning, log));
    }

    private async Task ValidateSequenceJob(CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(Properties, cancellationToken);
        if (!result.IsValid)
        {
            var message = "validate sequence job fail with the following errors:\r\n";
            message += string.Join("\r\n", result.Errors.Select(e => $" -{e.ErrorMessage}"));

            throw new InvalidDataException(message);
        }
    }
}