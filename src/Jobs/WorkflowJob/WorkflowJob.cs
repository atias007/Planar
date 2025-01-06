using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using System.Collections.Concurrent;

namespace Planar;

public abstract class WorkflowJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) :
    BaseCommonJob<WorkflowJobProperties>(logger, dataLayer, jobMonitorUtil),
    IWorkflowInstance
{
    private readonly ConcurrentDictionary<string, ResetEventWrapper> _resetEvents = [];

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            //ValidateWorkflowJob();
            StartMonitorDuration(context);
            var task = ExecuteWorkflow(context);
            await WaitForJobTask(context, task);
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            FinalizeJob(context);
        }
    }

    private async Task ExecuteWorkflow(IJobExecutionContext context)
    {
        var steps = Properties.Steps;
        var startStep = steps.First(s => s.DependsOnKey == null);
        await ExecuteStep(context, [startStep]);
    }

    private async Task ExecuteStep(IJobExecutionContext context, IEnumerable<WorkflowJobStep> steps)
    {
        if (!steps.Any()) { return; }

        var jobs = new List<(JobKey, JobDataMap)>();
        foreach (var step in steps)
        {
            var jobKey = ParseJobKey(step.Key);
            var data = GetJobDataMap(context, step);
            _resetEvents.TryAdd(jobKey.ToString(), ResetEventWrapper.Create(jobKey));
            jobs.Add((jobKey, data));
        }

        foreach (var tuple in jobs)
        {
            await context.Scheduler.TriggerJob(tuple.Item1, tuple.Item2);
        }

        var nextSteps = new ConcurrentBag<WorkflowJobStep>();
        Parallel.ForEach(_resetEvents, re =>
        {
            var wrapper = re.Value;
            wrapper.ResetEvent.WaitOne();
            var steps = Properties.Steps.Where(s =>
                s.DependsOnKey == wrapper.JobKey.ToString() &&
                s.DependsOnEvent == wrapper.Event);

            foreach (var step in steps)
            {
                nextSteps.Add(step);
            }
        });

        await ExecuteStep(context, nextSteps);
    }

    private static JobDataMap GetJobDataMap(IJobExecutionContext context, WorkflowJobStep step)
    {
        var data = context.MergedJobDataMap;
        foreach (var item in step.Data)
        {
            data.Add(item.Key, item.Value ?? string.Empty);
        }

        data.Add(Consts.WorkflowInstanceId, context.FireInstanceId);
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

    public void SignalEvent(JobKey jobKey, WorkflowJobStepEvent @event)
    {
        var key = jobKey.ToString();
        if (_resetEvents.TryGetValue(key, out var resetWrapper))
        {
            resetWrapper.Event = @event;
            resetWrapper.ResetEvent.Set();
        }
    }
}