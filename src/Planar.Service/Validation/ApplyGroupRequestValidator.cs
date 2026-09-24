using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public partial class ApplyGroupRequestValidator : AbstractValidator<ApplyGroupRequest>
{
    public ApplyGroupRequestValidator()
    {
        Include(new AddGroupRequestValidator());
        RuleForEach(g => g.Users).SetValidator(new ApplyGroupUserValidator());
    }
}
