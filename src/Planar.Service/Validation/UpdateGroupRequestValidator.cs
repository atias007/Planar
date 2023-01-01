using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
    {
        public UpdateGroupRequestValidator(IValidator<AddGroupRequest> addValidator)
        {
            Include(addValidator);
            RuleFor(e => e.Id).GreaterThan(0);
        }
    }
}