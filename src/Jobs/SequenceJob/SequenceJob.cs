using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.General;
using Quartz;
using System.Collections.Concurrent;
using System.Globalization;

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
    private readonly ConcurrentDictionary<string, ResetEventWrapper> _resetEvents = [];

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            await ValidateSequenceJob(ExecutionCancellationToken);
            StartMonitorDuration(context);
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
            SafeInvoke(_resetEvents.Clear);
        }
    }

    public bool SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, int index, SequenceJobStepEvent @event)
    {
        var key = ResetEventWrapper.GetKey(stepJobKey, sequenceFireInstanceId, index);
        if (_resetEvents.TryGetValue(key, out var resetWrapper))
        {
            resetWrapper.Event = @event;
            resetWrapper.FireInstanceId = fireInstanceId;
            resetWrapper.ResetEvent.Set();
            return true;
        }

        return false;
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

        var data = _resetEvents.Values;
        if (data.Count == 0) { return; }

        AppendInfoLog("  ");
        AppendInfoLog(Seperator);
        AppendInfoLog(" sequence steps summary");
        AppendInfoLog(Seperator);
        AppendInfoLog("[job key] --> [fire instance id] --> [status]");
        AppendInfoLog(string.Empty);
        foreach (var wrapper in data)
        {
            var text = $"{wrapper.JobKey} --> {wrapper.FireInstanceId} --> {wrapper.DisplayStatus}";
            if (wrapper.Event == SequenceJobStepEvent.Fail)
            {
                AppendWarnLog(text);
            }
            else
            {
                AppendInfoLog(text);
            }
        }

        AppendInfoLog(Seperator);
    }

    private static JobDataMap GetJobDataMap(IJobExecutionContext context, SequenceJobStep step, int index)
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
        data.TryAdd(Consts.SequenceInstanceIndexDataKey, index.ToString(CultureInfo.CurrentCulture));
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
        if (cancellationToken.IsCancellationRequested) { return; }

        var completedSteps = 0;
        var totalSteps = steps.Count();

        // build steps wrapper
        foreach (var (index, step) in steps.Index())
        {
            var jobKey = ParseJobKey(step.Key);
            var data = GetJobDataMap(context, step, index);
            var wrapper = ResetEventWrapper.Create(jobKey, context.FireInstanceId, data, step, index);
            _resetEvents.TryAdd(wrapper.Key, wrapper);
        }

        // run steps
        foreach (var wrapper in _resetEvents.Values)
        {
            // prepare
            var timeout = wrapper.Timeout ?? AppSettings.General.JobAutoStopSpan;
            if (wrapper.Status != StepStatus.Waiting) { return; }
            MessageBroker.AppendLog(LogLevel.Information, $" - [step {wrapper.Index}] start job: {wrapper.JobKey}");

            // execute step
            wrapper.SetStatus(StepStatus.Start);
            await context.Scheduler.TriggerJob(wrapper.JobKey, wrapper.DataMap, cancellationToken);
            var result = wrapper.ResetEvent.WaitOne(timeout);

            // handle step timeout
            if (!result)
            {
                // TODO: handle timeout
                return;
            }

            // check for cancellation token
            if (cancellationToken.IsCancellationRequested)
            {
                wrapper.SetStatus(StepStatus.Interrupted);
                return;
            }

            // update status
            wrapper.SetStatus(StepStatus.Finish);
            completedSteps++;
            MessageBroker.IncreaseEffectedRows(1);
            MessageBroker.UpdateProgress(completedSteps, totalSteps);

            // check for cancellation token
            if (cancellationToken.IsCancellationRequested) { return; }
        }
    }

    private async Task ExecuteSequence(IJobExecutionContext context)
    {
        // register cancellation token event
        ExecutionCancellationToken.Register(() =>
        {
            _resetEvents.Values.ToList().ForEach(w => w.ResetEvent.Set());
        });

        // find startup step
        var steps = Properties.Steps;
        MessageBroker.AppendLog(LogLevel.Information, $"start sequence with {steps.Count} steps");

        // start sequence
        await ExecuteSteps(context, Properties.Steps, ExecutionCancellationToken);

        // handle cancel steps
        ExecutionCancellationToken.ThrowIfCancellationRequested();
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