using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public partial class ApplyGroupRequestValidator : AbstractValidator<ApplyGroupRequest>
{
    public ApplyGroupRequestValidator()
    {
        Include(new AddGroupRequestValidator());
        RuleForEach(g => g.Users).NotEmpty().Length(2, 50)
            .Matches(AddUserRequestValidator.AllowedRegex()).WithMessage("Username can only contain letters, numbers, dots, spaces, underscores and hyphens");
    }
}