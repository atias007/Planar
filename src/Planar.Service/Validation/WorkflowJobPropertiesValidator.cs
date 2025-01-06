using FluentValidation;

namespace Planar.Service.Validation;

public class WorkflowJobPropertiesValidator : AbstractValidator<WorkflowJobProperties>
{
    public WorkflowJobPropertiesValidator()
    {
        RuleFor(e => e.Steps).NotEmpty();
        RuleForEach(e => e.Steps).SetValidator(new WorkflowJobStepValidator());
    }
}

public class WorkflowJobStepValidator : AbstractValidator<WorkflowJobStep>
{
    public WorkflowJobStepValidator()
    {
        RuleFor(e => e.Key).NotEmpty().MinimumLength(7).MaximumLength(101);
        RuleFor(e => e.DependsOnKey).MinimumLength(7).MaximumLength(101);
        RuleFor(e => e.DependsOnEvent).NotEmpty();
        RuleFor(e => e.Timeout).NotZero();
    }
}