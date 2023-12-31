using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.General;
using System;
using System.Linq;

namespace Planar.Service.Validation
{
    public class GetAllJobsRequestValidator : AbstractValidator<GetAllJobsRequest>
    {
        public GetAllJobsRequestValidator()
        {
            RuleFor(r => r.Group).MaximumLength(50);
            RuleFor(r => r.JobType).MaximumLength(50);
            RuleFor(r => r.Filter).MaximumLength(50);

            RuleFor(r => r.JobType)
                .Must(r => ServiceUtil.JobTypes.Any(t => string.Equals(r, t, StringComparison.OrdinalIgnoreCase)))
                .When(r => !string.IsNullOrEmpty(r.JobType))
                .WithMessage("'{PropertyValue}' is invalid job type");

            RuleFor(r => r.Group)
                .Must(string.IsNullOrEmpty)
                .When(r => r.JobCategory == AllJobsMembers.AllSystemJobs)
                .WithMessage("group property must be null when filter is: system jobs");
        }
    }
}