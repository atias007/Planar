using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class UpsertJobPropertyRequestValidator : AbstractValidator<UpsertJobPropertyRequest>
    {
        public UpsertJobPropertyRequestValidator()
        {
            RuleFor(r => r.Id).NotEmpty();
            RuleFor(r => r.PropertyValue).NotEmpty();
            RuleFor(r => r.PropertyKey).NotEmpty();
        }
    }
}