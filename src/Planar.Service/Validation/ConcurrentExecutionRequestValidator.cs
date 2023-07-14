using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class ConcurrentExecutionRequestValidator : AbstractValidator<ConcurrentExecutionRequest>
    {
        public ConcurrentExecutionRequestValidator()
        {
            Include(new MaxConcurrentExecutionRequestValidator());
            RuleFor(c => c.Server).MaximumLength(100);
            RuleFor(c => c.InstanceId).MaximumLength(100);
        }
    }
}