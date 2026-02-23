using CommonJob;
using FluentValidation;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class SequenceJobPropertiesValidator : AbstractValidator<SequenceJobProperties>
{
    public SequenceJobPropertiesValidator(ISchedulerFactory schedulerFactory)
    {
        RuleFor(e => e.Steps).NotEmpty();
        RuleFor(e => e.Steps)
            .Must(e => e.Count <= 100)
            .WithMessage("no more than total 100 steps are allowed");

        RuleForEach(e => e.Steps)
            .SetValidator(new SequenceJobStepValidator(schedulerFactory))
            .WithMessage("fail to validate sequence steps");
    }
}

public class SequenceJobStepValidator : AbstractValidator<SequenceJobStep>
{
    public SequenceJobStepValidator(ISchedulerFactory schedulerFactory)
    {
        RuleFor(e => e.Key)
            .MustAsync((key, ct) => IsJobExistsAsync(schedulerFactory, key, ct))
            .WithMessage("job with key '{PropertyValue}' does not exist");
        RuleFor(e => e.Key).NotEmpty().MinimumLength(7).MaximumLength(101);

        RuleFor(e => e.Timeout).NotZero().LessThanOrEqualTo(TimeSpan.FromDays(1));
        RuleFor(e => e.Data).Must(ValidateDataAsync);
    }

    public static async Task<bool> IsJobExistsAsync(ISchedulerFactory schedulerFactory, string? jobKey, CancellationToken cancellationToken)
    {
        if (jobKey == null) { return true; }
        var helper = new JobKeyHelper(schedulerFactory);
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

    public static bool ValidateDataAsync(SequenceJobStep step, Dictionary<string, string?> data, ValidationContext<SequenceJobStep> context)
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