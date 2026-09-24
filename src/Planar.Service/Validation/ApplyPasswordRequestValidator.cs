using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Validation;

namespace Planar.Service;

public partial class ApplyPasswordRequestValidator : AbstractValidator<ApplyPasswordRequest>
{
    public ApplyPasswordRequestValidator()
    {
        Include(new SetPasswordRequestValidator());

        RuleFor(r => r.Username)
            .NotEmpty()
            .Length(2, 50)
            .Matches(AddUserRequestValidator.AllowedRegex())
            .WithMessage("Username can only contain letters, numbers, dots, underscores and hyphens");
    }
}