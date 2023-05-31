using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateEntityRequestByIdValidator : AbstractValidator<UpdateEntityRequestById>
    {
        public UpdateEntityRequestByIdValidator()
        {
            RuleFor(u => u.Id).GreaterThan(0);
            RuleFor(u => u.PropertyName).NotEmpty().NotEqual("id", StringIgnoreCaseComparer.Instance);
            RuleFor(u => u.PropertyValue).NotEmpty();
        }
    }
}