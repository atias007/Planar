using FluentValidation;

namespace Planar.Service.Validation
{
    internal class RestJobBasicAuthenticationValidator : AbstractValidator<RestJobBasicAuthentication>
    {
        public RestJobBasicAuthenticationValidator()
        {
            RuleFor(r => r.Password).NotEmpty().MaximumLength(1000);
            RuleFor(r => r.Username).NotEmpty().MaximumLength(1000);
        }
    }
}