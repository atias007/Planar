using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class FireInstanceIdRequestValidator : AbstractValidator<FireInstanceIdRequest>
    {
        public FireInstanceIdRequestValidator()
        {
            RuleFor(r => r.FireInstanceId).NotEmpty();
        }
    }
}