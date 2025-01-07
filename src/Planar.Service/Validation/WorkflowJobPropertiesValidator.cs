using CommonJob;
using FluentValidation;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Quartz;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class WorkflowJobPropertiesValidator : AbstractValidator<WorkflowJobProperties>
{
    public WorkflowJobPropertiesValidator(IScheduler scheduler)
    {
        RuleFor(e => e.Steps).NotEmpty();
        RuleFor(e => e.Steps)
            .Must(HasStartStep)
            .WithMessage("workflow has no start step. a step with 'depends on key: null' must be define");

        RuleFor(e => e.Steps)
            .Must(HasSingleStartStep)
            .WithMessage("workflow has multiple start steps. only one step with 'depends on key: null' must be define");

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

        RuleFor(e => e.Timeout).NotZero();
        RuleFor(e => e.Data).MustAsync(ValidateDataAsync);
    }

    public static async Task<bool> IsJobExistsAsync(IScheduler scheduler, string? jobKey, CancellationToken cancellationToken)
    {
        if (jobKey == null) { return true; }
        var helper = new JobKeyHelper(scheduler);
        var key = await helper.GetJobKey(jobKey);
        var exists = await scheduler.GetJobDetail(key, cancellationToken);
        return exists != null;
    }

    public static async Task<bool> ValidateDataAsync(WorkflowJobStep step, Dictionary<string, string> data, ValidationContext<WorkflowJobStep> context, CancellationToken cancellationToken = default)
    {
        try
        {
            JobDomain.ValidateDataMap(data, string.Empty);
        }
        catch (System.Exception ex)
        {
            throw;
        }
    }
}