using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Quartz;
using System.Collections.Concurrent;

namespace Planar;

public abstract class WorkflowJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IValidator<WorkflowJobProperties> validator) :
    BaseCommonJob<WorkflowJobProperties>(logger, dataLayer, jobMonitorUtil),
    IWorkflowInstance
{
    private readonly ConcurrentDictionary<string, ResetEventWrapper> _resetEvents = [];
    private int _completedSteps;
    private int _recursiveLevel = 1;
    private int _totalSteps;

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            await ValidateWorkflowJob(ExecutionCancellationToken);
            StartMonitorDuration(context);
            WorkflowManager.RegisterWorkflow(context.FireInstanceId, this);
            var task = ExecuteWorkflow(context);
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
            WorkflowManager.UnregisterWorkflow(context.FireInstanceId);
            FinalizeJob(context);
            _resetEvents.Clear();
        }
    }

    public void SignalEvent(JobKey stepJobKey, string workflowFireInstanceId, WorkflowJobStepEvent @event)
    {
        var key = ResetEventWrapper.GetKey(stepJobKey, workflowFireInstanceId);
        if (_resetEvents.TryGetValue(key, out var resetWrapper))
        {
            resetWrapper.Event = @event;
            resetWrapper.ResetEvent.Set();
        }
    }

    private static JobDataMap GetJobDataMap(IJobExecutionContext context, WorkflowJobStep step)
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

        data.TryAdd(Consts.WorkflowInstanceIdDataKey, context.FireInstanceId);
        data.TryAdd(Consts.WorkflowJobKeyDataKey, context.JobDetail.Key.ToString());
        data.TryAdd(Consts.WorkflowTriggerIdDataKey, triggerId);

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

    private async Task ExecuteSteps(IJobExecutionContext context, IEnumerable<WorkflowJobStep> steps, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) { return; }

        if (!steps.Any())
        {
            _recursiveLevel--;
            return;
        }

        // build steps wrapper
        foreach (var step in steps)
        {
            var jobKey = ParseJobKey(step.Key);
            var data = GetJobDataMap(context, step);
            var wrapper = ResetEventWrapper.Create(jobKey, context.FireInstanceId, data, step);
            _resetEvents.TryAdd(wrapper.Key, wrapper);
        }

        // run steps
        var nextSteps = new ConcurrentBag<WorkflowJobStep>();
        await Parallel.ForEachAsync(_resetEvents.Values, cancellationToken, async (wrapper, cancellationToken) =>
        {
            // prepare
            var timeout = wrapper.Timeout ?? AppSettings.General.JobAutoStopSpan;
            if (wrapper.Done) { return; }
            var ident = string.Empty.PadLeft(_recursiveLevel * 2, ' ');
            MessageBroker.AppendLog(LogLevel.Information, $"{ident}└─ start job '{wrapper.JobKey}'");

            // execute step
            await context.Scheduler.TriggerJob(wrapper.JobKey, wrapper.DataMap, cancellationToken);
            var result = wrapper.ResetEvent.WaitOne(timeout);

            // handle step timeout
            if (!result)
            {
                // TODO: stop parallel loop and throw exception
            }

            // check for cancellation token
            if (cancellationToken.IsCancellationRequested) { return; }

            // update status
            wrapper.SetDone();
            _resetEvents.TryRemove(wrapper.Key, out _);
            var completed = Interlocked.Increment(ref _completedSteps);
            MessageBroker.IncreaseEffectedRows(1);
            MessageBroker.UpdateProgress(completed, _totalSteps);

            // check for cancellation token
            if (cancellationToken.IsCancellationRequested) { return; }

            // collect next steps
            var steps = Properties.Steps.Where(s =>
                s.DependsOnKey == wrapper.JobKey.ToString() &&
                (s.DependsOnEvent == wrapper.Event || s.DependsOnEvent == WorkflowJobStepEvent.Finish));

            foreach (var step in steps)
            {
                nextSteps.Add(step);
            }
        });

        // execute next steps
        if (cancellationToken.IsCancellationRequested) { return; }
        _recursiveLevel++;
        await ExecuteSteps(context, nextSteps, cancellationToken);
    }

    private async Task ExecuteWorkflow(IJobExecutionContext context)
    {
        await Task.Yield();

        // register cancellation token event
        ExecutionCancellationToken.Register(() =>
        {
            _resetEvents.Values.ToList().ForEach(w => w.ResetEvent.Set());
        });

        // find startup step
        var steps = Properties.Steps;
        _totalSteps = steps.Count;
        var startStep = steps.First(s => s.DependsOnKey == null);
        MessageBroker.AppendLog(LogLevel.Information, $"start workflow with {steps.Count} steps");

        // start workflow
        await ExecuteSteps(context, [startStep], ExecutionCancellationToken);

        // handle cancel steps
        ExecutionCancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ValidateWorkflowJob(CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(Properties, cancellationToken);
        if (!result.IsValid)
        {
            var message = "validate workflow job fail with the following errors:\r\n";
            message += string.Join("\r\n", result.Errors.Select(e => $" -{e.ErrorMessage}"));

            throw new InvalidDataException(message);
        }
    }
}