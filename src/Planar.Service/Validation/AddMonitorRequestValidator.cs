using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
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
            RuleFor(r => r.EventName).NotEmpty().IsInEnum(typeof(MonitorEvents));

            RuleFor(r => r.JobGroup)
                .Must((r, g, c) => JobAndGroupExists(r, jobKeyHelper, c))
                .WithMessage("{Message}");

            RuleFor(r => r.JobGroup).NotEmpty()
                .When(r => !string.IsNullOrWhiteSpace(r.JobName))
                .WithMessage("{PropertyName} must have value if Job Name is not empty");

            RuleFor(r => r.JobGroup).Empty()
                .When(r => MonitorEventsExtensions.IsSystemMonitorEvent(r.EventName))
                .WithMessage(r => $"{{PropertyName}} must be null when Event Name is {r.EventName?.SplitWords()}");

            RuleFor(r => r.GroupName).NotEmpty();

            RuleFor(r => r.GroupName)
                .Must(g => dal.IsGroupNameExists(g).Result)
                .When(r => !string.IsNullOrWhiteSpace(r.GroupName))
                .WithMessage("{PropertyName} '{PropertyValue}' does not exist");

            RuleFor(r => r.Hook).NotEmpty();

            RuleFor(r => r.Hook)
                .Must(ValidationUtil.IsHookExists)
                .When(r => !string.IsNullOrWhiteSpace(r.Hook))
                .WithMessage("{PropertyName} '{PropertyValue}' does not exist");

            RuleFor(r => r.EventArgument).NotEmpty()
                .When(r => !string.IsNullOrWhiteSpace(r.EventName) && MonitorEventsExtensions.IsMonitorEventHasArguments(r.EventName))
                .WithMessage(r => $"{{PropertyName}} must have value when Event Id is {r.EventName?.SplitWords()}");

            RuleFor(r => r.EventArgument).Empty()
                .When(r => !string.IsNullOrWhiteSpace(r.EventName) && !MonitorEventsExtensions.IsMonitorEventHasArguments(r.EventName))
                .WithMessage(r => $"{{PropertyName}} must be empty when Event Id is {r.EventName?.SplitWords()}");

            RuleFor(r => r.JobName).NotEmpty()
                .When(r => MonitorEventsExtensions.IsMonitorEventHasArguments(r.EventName))
                .WithMessage(r => $"{{PropertyName}} and Job Group must have value when 'Event Id' is {r.EventName?.SplitWords()}");

            RuleFor(r => r.JobName).Empty()
                .When(r => MonitorEventsExtensions.IsSystemMonitorEvent(r.EventName))
                .WithMessage(r => $"{{PropertyName}} must be null when 'Event Id' is {r.EventName?.SplitWords()}");
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
                jobKeyHelper.ValidateJobExists(key).Wait();
                return true;
            }
            catch (AggregateException ex)
            {
                if (ex.Flatten().InnerException is RestNotFoundException)
                {
                    context.MessageFormatter.AppendArgument("Message", $"job with key '{KeyHelper.GetKeyTitle(key)}' is not exists");
                    return false;
                }

                throw;
            }
        }
    }
}