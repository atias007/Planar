using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public partial class ApplyGroupUserValidator : AbstractValidator<ApplyGroupUser>
{
    public ApplyGroupUserValidator()
    {
        RuleFor(r => r.Username).NotEmpty().Length(2, 50)
            .Matches(AddUserRequestValidator.AllowedRegex()).WithMessage("Username can only contain letters, numbers, dots, spaces, underscores and hyphens");
    }
}