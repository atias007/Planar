using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateEntityRequestByNameValidator : AbstractValidator<UpdateEntityRequestByName>
    {
        public UpdateEntityRequestByNameValidator()
        {
            RuleFor(u => u.Name).NotEmpty().Length(2, 50);
            RuleFor(u => u.PropertyValue).NotEmpty();
        }
    }
}