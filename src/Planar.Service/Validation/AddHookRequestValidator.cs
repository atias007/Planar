using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

internal class AddHookRequestValidator : AbstractValidator<AddHookRequest>
{
    public AddHookRequestValidator()
    {
        RuleFor(e => e.Filename).NotEmpty().MaximumLength(1000);
    }
}