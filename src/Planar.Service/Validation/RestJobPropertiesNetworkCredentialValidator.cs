using FluentValidation;

namespace Planar.Service.Validation
{
    internal class RestJobPropertiesNetworkCredentialValidator : AbstractValidator<RestJobPropertiesNetworkCredential>
    {
        public RestJobPropertiesNetworkCredentialValidator()
        {
            RuleFor(r => r.Password).NotEmpty().MaximumLength(100);
            RuleFor(r => r.Username).NotEmpty().MaximumLength(100);
            RuleFor(r => r.Domain).MaximumLength(100);
        }
    }
}