using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class GlobalConfigApplyRequestValidator : AbstractValidator<GlobalConfigApplyRequest>
{
    public GlobalConfigApplyRequestValidator()
    {
        Include(new GlobalConfigDataUpdateValidator());
    }
}