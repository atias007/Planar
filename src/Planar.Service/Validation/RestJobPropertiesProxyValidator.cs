using FluentValidation;
using System;

namespace Planar.Service.Validation;

internal class RestJobPropertiesProxyValidator : AbstractValidator<RestJobPropertiesProxy>
{
    public RestJobPropertiesProxyValidator()
    {
        RuleFor(r => r.Address)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of proxy 'address' must be 1000 characters or fewer. You entered {e.Address?.Length ?? 0} characters");

        RuleFor(r => r.Address)
            .Must(r => Uri.TryCreate(r, UriKind.Absolute, out _))
            .WithMessage("proxy 'address' with value '{PropertyValue}' is not valid url");

        RuleFor(r => r.Credentials)
            .Null()
            .When(r => r.UseDefaultCredentials)
            .WithMessage("proxy 'credentials' must be null when 'use default credentials' is true");

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        RuleFor(r => r.Credentials)
            .SetValidator(new RestJobPropertiesNetworkCredentialValidator())
            .When(r => r.Credentials != null);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
    }
}