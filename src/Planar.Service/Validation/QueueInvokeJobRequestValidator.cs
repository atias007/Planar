using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class QueueInvokeJobRequestValidator : AbstractValidator<QueueInvokeJobRequest>
    {
        public QueueInvokeJobRequestValidator()
        {
            RuleFor(r => r.DueDate).NotEmpty().GreaterThan(DateTime.Now);
            RuleFor(r => r.Timeout).GreaterThan(TimeSpan.Zero);
        }
    }
}