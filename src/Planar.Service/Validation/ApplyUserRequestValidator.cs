using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service;

public partial class ApplyUserRequestValidator : AbstractValidator<ApplyUserRequest>
{
    public ApplyUserRequestValidator()
    {
        Include(new AddUserRequestValidator());
    }
}
