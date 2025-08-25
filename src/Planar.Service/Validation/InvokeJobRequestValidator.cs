using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class InvokeJobRequestValidator : AbstractValidator<InvokeJobRequest>
{
    public InvokeJobRequestValidator()
    {
        Include(new JobOrTriggerKeyValidator());
        RuleFor(x => x.NowOverrideValue).ValidSqlDateTime();
        RuleFor(x => x.RetrySpan)
            .NotEmpty()
            .When(x => x.MaxRetries.HasValue)
            .WithMessage("Must specify a retry span when max retries is set");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .When(x => x.RetrySpan.HasValue)
            .WithMessage("Must specify max retries when a retry span is set");
    }
}