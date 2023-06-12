using FluentValidation;
using System;

namespace Planar.Service.Validation
{
    internal class RestJobPropertiesProxyValidator : AbstractValidator<RestJobPropertiesProxy>
    {
        public RestJobPropertiesProxyValidator()
        {
            RuleFor(r => r.Address)
                .MaximumLength(1000)
                .Must(r => Uri.TryCreate(r, UriKind.Absolute, out _))
                .WithMessage("proxy address '{PropertyValue}' is not valid url");

            RuleFor(r => r.Credentials)
                .Null()
                .When(r => r.UseDefaultCredentials)
                .WithMessage("credentials must be null when use default credentials is true");

            RuleFor(r => r.Credentials).SetValidator(new RestJobPropertiesNetworkCredentialValidator()!).When(r => r.Credentials != null);
        }
    }
}