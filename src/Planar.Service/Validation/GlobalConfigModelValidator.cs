using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class GlobalConfigModelValidator : AbstractValidator<GlobalConfigModel>
{
    public GlobalConfigModelValidator()
    {
        Include(new GlobalConfigDataValidator());
    }
}
