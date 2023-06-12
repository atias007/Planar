using FluentValidation;

namespace Planar.Service.Validation
{
    internal class RestJobJwtAuthenticationValidator : AbstractValidator<RestJobJwtAuthentication>
    {
        public RestJobJwtAuthenticationValidator()
        {
            RuleFor(r => r.Token).NotEmpty().MaximumLength(1000);
        }
    }
}