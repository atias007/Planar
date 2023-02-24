using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class CounterRequestValidator : AbstractValidator<CounterRequest>
    {
        public CounterRequestValidator()
        {
            RuleFor(c => c.Hours).InclusiveBetween(1, 720);
        }
    }
}