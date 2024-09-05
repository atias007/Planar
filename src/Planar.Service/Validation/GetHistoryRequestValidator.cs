using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation;

public class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        Include(new PagingRequestValidator());
        RuleFor(r => r.FromDate).ValidSqlDateTime().LessThan(DateTime.Now);
        RuleFor(r => r.ToDate).ValidSqlDateTime();
        RuleFor(r => r.FromDate).LessThan(r => r.ToDate).When(r => r.FromDate.HasValue && r.ToDate.HasValue);

        RuleFor(r => r.JobId).Null()
            .When((req, r) => !string.IsNullOrEmpty(req.JobGroup))
            .WithMessage("{PropertyName} must be null when 'Group' property is provided");

        RuleFor(r => r.JobGroup).Null()
            .When((req, r) => !string.IsNullOrEmpty(req.JobId))
            .WithMessage("{PropertyName} must be null when 'JobId' property is provided");

        RuleFor(r => r.JobId).MaximumLength(101);
        RuleFor(r => r.JobGroup).MaximumLength(50);
    }
}