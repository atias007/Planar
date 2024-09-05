using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.General;
using System.Linq;

namespace Planar.Service.Validation;

public class GetLastHistoryCallForJobRequestValidator : AbstractValidator<GetLastHistoryCallForJobRequest>
{
    public GetLastHistoryCallForJobRequestValidator()
    {
        Include(new PagingRequestValidator());
        RuleFor(e => e.LastDays).InclusiveBetween(0, 356);

        RuleFor(r => r.JobId).Null()
            .When((req, r) => !string.IsNullOrEmpty(req.JobGroup))
            .WithMessage("{PropertyName} must be null when 'Group' property is provided");

        RuleFor(r => r.JobGroup).Null()
            .When((req, r) => !string.IsNullOrEmpty(req.JobId))
            .WithMessage("{PropertyName} must be null when 'JobId' property is provided");

        RuleFor(r => r.JobType)
            .Must(t =>
            {
                return ServiceUtil.JobTypes.Any(j => string.Equals(t, j, System.StringComparison.OrdinalIgnoreCase));
            })
            .When(r => !string.IsNullOrEmpty(r.JobType))
            .WithMessage("Invalid job type {PropertyValue}");

        RuleFor(r => r.JobId).MaximumLength(101);
        RuleFor(r => r.JobGroup).MaximumLength(50);
    }
}