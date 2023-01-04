using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Quartz;
using System;

namespace Planar.Service.Validation
{
    public class AddMonitorRequestValidator : AbstractValidator<AddMonitorRequest>
    {
        public AddMonitorRequestValidator(GroupData dal, JobKeyHelper jobKeyHelper)
        {
            RuleFor(r => r.Title).NotEmpty().Length(5, 50);
            RuleFor(r => r.EventArgument).MaximumLength(50);
            RuleFor(r => r.EventId).NotEmpty().IsInEnum();

            RuleFor(r => r.JobGroup)
                .Must((r, g, c) => JobAndGroupExists(r, jobKeyHelper, c))
                .WithMessage("{Message}");

            RuleFor(r => r.JobGroup).NotEmpty()
                .When(r => !string.IsNullOrEmpty(r.JobName))
                .WithMessage("{PropertyName} must have value if 'Job Name' is not empty");

            RuleFor(r => r.GroupId).GreaterThan(0)
                .Must(g => dal.IsGroupExists(g).Result)
                .WithMessage("'{PropertyName}' field with value '{PropertyValue}' does not exist");

            RuleFor(r => r.Hook).NotEmpty()
                .Must(ValidationUtil.IsHookExists)
                .WithMessage("'{PropertyName}' field with value '{PropertyValue}' does not exist");

            RuleFor(r => r.EventArgument).NotEmpty()
                .When(r => (int)r.EventId >= (int)MonitorEvents.ExecutionFailxTimesInRow)
                .WithMessage(r => $"'{{PropertyName}}' must have value while event is {r.EventId}");

            RuleFor(r => r.EventArgument).Empty()
                .When(r => (int)r.EventId < (int)MonitorEvents.ExecutionFailxTimesInRow)
                .WithMessage(r => $"'{{PropertyName}}' must have be empty while event is {r.EventId}");

            RuleFor(r => r.JobName).NotEmpty()
                .When(r => (int)r.EventId >= (int)MonitorEvents.ExecutionFailxTimesInRow)
                .WithMessage(r => $"'Job Name' and 'Job Group' must have value while event is {r.EventId}");
        }

        private static bool JobAndGroupExists(AddMonitorRequest request, JobKeyHelper jobKeyHelper, ValidationContext<AddMonitorRequest> context)
        {
            if (string.IsNullOrEmpty(request.JobGroup) && string.IsNullOrEmpty(request.JobName)) { return true; }

            if (string.IsNullOrEmpty(request.JobName))
            {
                var groupKey = JobKey.Create("test", request.JobGroup);
                var result = jobKeyHelper.IsJobGroupExists(groupKey.Group).Result;
                if (!result)
                {
                    context.MessageFormatter.AppendArgument("Message", $"'Job Group' {groupKey.Group} is not exists");
                }

                return result;
            }

            var key = JobKey.Create(request.JobName, request.JobGroup);
            try
            {
                jobKeyHelper.GetJobKey($"{key.Group}.{key.Name}").Wait();
                return true;
            }
            catch (AggregateException ex)
            {
                if (ex.Flatten().InnerException is RestNotFoundException)
                {
                    context.MessageFormatter.AppendArgument("Message", $"job {key.Group}.{key.Name} is not exists");
                    return false;
                }

                throw;
            }
        }
    }
}