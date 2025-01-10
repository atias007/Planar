using CommonJob;
using FluentValidation;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Polly;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class WorkflowJobPropertiesValidator : AbstractValidator<WorkflowJobProperties>
{
    private int _recursiveLevel = 1;

    public WorkflowJobPropertiesValidator(IScheduler scheduler)
    {
        RuleFor(e => e.Steps).NotEmpty();
        RuleFor(e => e.Steps)
            .Must(HasStartStep)
            .WithMessage("workflow has no start step. a step with 'depends on key: null' must be define");

        RuleFor(e => e.Steps)
            .Must(HasSingleStartStep)
            .WithMessage("workflow has multiple start steps. only one step with 'depends on key: null' must be define");

        RuleFor(e => e).Must(IsDepentOnKeyExistsInWorkflow);
        RuleFor(e => e).Must(GetRecursiveLevel);

        RuleForEach(e => e.Steps).SetValidator(new WorkflowJobStepValidator(scheduler));
    }

    private static bool HasStartStep(IEnumerable<WorkflowJobStep> steps)
    {
        return steps.Any(s => s.DependsOnKey == null);
    }

    private static bool HasSingleStartStep(IEnumerable<WorkflowJobStep> steps)
    {
        return steps.Count(s => s.DependsOnKey == null) == 1;
    }

    private static bool IsDepentOnKeyExistsInWorkflow(WorkflowJobProperties properties1, WorkflowJobProperties properties2, ValidationContext<WorkflowJobProperties> context)
    {
        var dependKeys = properties1.Steps.Select(s => s.DependsOnKey!).Where(k => k != null).ToList();
        var keys = properties1.Steps.Select(s => s.Key).ToList();
        var missingKeys = dependKeys.Where(k => !keys.Contains(k)).ToList();
        if (missingKeys.Count > 0)
        {
            context.AddFailure($"missing 'depends on key' in workflow: {string.Join(", ", missingKeys)}");
            return false;
        }

        return true;
    }

    private bool GetRecursiveLevel(WorkflowJobProperties properties1, WorkflowJobProperties properties2, ValidationContext<WorkflowJobProperties> context)
    {
        _recursiveLevel = 1;
        var startStep = properties1.Steps.FirstOrDefault(s => s.DependsOnKey == null);
        if (startStep == null) { return true; }
        DemoExecuteSteps(properties1, [startStep]);
        if (_recursiveLevel <= 100) { return true; }

        context.AddFailure("workflow has more than 100 recursive levels");
        return false;
    }

    private void DemoExecuteSteps(WorkflowJobProperties properties, IEnumerable<WorkflowJobStep> steps)
    {
        if (!steps.Any())
        {
            _recursiveLevel--;
            return;
        }

        var nextSteps = new List<WorkflowJobStep>();
        foreach (var step in steps)
        {
            // collect next steps
            var temp = properties.Steps.Where(s => s.DependsOnKey == step.Key).ToList();
            nextSteps.AddRange(temp);
        }

        _recursiveLevel++;
        DemoExecuteSteps(properties, nextSteps);
    }
}

public class WorkflowJobStepValidator : AbstractValidator<WorkflowJobStep>
{
    public WorkflowJobStepValidator(IScheduler scheduler)
    {
        RuleFor(e => e.Key)
            .MustAsync((key, ct) => IsJobExistsAsync(scheduler, key, ct))
            .When(e => e.DependsOnKey != null)
            .WithMessage("job with key '{PropertyValue}' does not exist");
        RuleFor(e => e.Key).NotEmpty().MinimumLength(7).MaximumLength(101);

        RuleFor(e => e.DependsOnKey).MinimumLength(7).MaximumLength(101);
        RuleFor(e => e.DependsOnKey).MustAsync((key, ct) => IsJobExistsAsync(scheduler, key, ct))
            .When(e => e.DependsOnKey != null)
            .WithMessage("job with key '{PropertyValue}' does not exist");
        RuleFor(e => e.DependsOnKey).Null()
            .When(e => e.DependsOnEvent == null)
            .WithMessage("'depends on key' must be null when 'depends on event' is null");

        RuleFor(e => e.DependsOnEvent).IsInEnum(typeof(WorkflowJobStepEvent));
        RuleFor(e => e.DependsOnEvent).Null()
            .When(e => e.DependsOnKey == null)
            .WithMessage("'depends on event' must be null when 'depends on key' is null");

        RuleFor(e => e.Timeout).NotZero().LessThanOrEqualTo(TimeSpan.FromDays(1));
        RuleFor(e => e.Data).Must(ValidateDataAsync);
    }

    public static async Task<bool> IsJobExistsAsync(IScheduler scheduler, string? jobKey, CancellationToken cancellationToken)
    {
        if (jobKey == null) { return true; }
        var helper = new JobKeyHelper(scheduler);
        try
        {
            await helper.GetJobKey(jobKey);
            return true;
        }
        catch (RestNotFoundException)
        {
            return false;
        }
    }

    public static bool ValidateDataAsync(WorkflowJobStep step, Dictionary<string, string?> data, ValidationContext<WorkflowJobStep> context)
    {
        try
        {
            JobDomain.ValidateDataMap(data, string.Empty);
            return true;
        }
        catch (RestValidationException ex)
        {
            ex.Errors.SelectMany(e => e.Detail).ToList().ForEach(context.AddFailure);
            return false;
        }
    }
}