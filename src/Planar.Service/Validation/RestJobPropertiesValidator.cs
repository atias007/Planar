using FluentValidation;
using Planar.Service.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class RestJobPropertiesValidator : AbstractValidator<RestJobProperties>
    {
        private static readonly string[] _methods = new[] { "POST", "GET", "PUT", "DELETE", "PATCH", "HEAD" };
        private readonly ClusterUtil _cluster;

        public RestJobPropertiesValidator(ClusterUtil cluster)
        {
            _cluster = cluster;

            RuleFor(r => r.Url)
                .NotEmpty()
                .MaximumLength(1000)
                .Must(r => Uri.TryCreate(r, UriKind.Absolute, out _))
                .WithMessage("'{PropertyValue}' is not valid url");

            RuleFor(r => r.Method)
                .NotEmpty()
                .Must(r => _methods.Any(m => string.Equals(r, m, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("methot '{PropertyValue}' is invalid. avaliable options are: " + string.Join(',', _methods));

            RuleFor(r => r.BodyFile).MaximumLength(1000).MustAsync(FilenameExists);
            RuleFor(r => r.BodyFile)
                .Empty()
                .When(r => r.Method == "GET" || r.Method == "HEAD" || (r.Headers?.Any() ?? true))
                .WithMessage("body filename must be null when method is GET or HEAD");

            RuleFor(r => r.UserAgent).MaximumLength(1000);
            RuleFor(r => r.MaxRedirects).GreaterThan(0).When(r => r.MaxRedirects.HasValue).WithMessage("max redirects must be greater the 0");
            RuleFor(r => r.MaxRedirects).NotEmpty().When(r => r.FollowRedirects).WithMessage("max redirects must have value when follow redirects is true");
            RuleFor(r => r.BasicAuthentication).Null().When(r => r.JwtAuthentication != null).WithMessage("basic authentication must be null when jwt authentication has value");
            RuleFor(r => r.BasicAuthentication).SetValidator(new RestJobBasicAuthenticationValidator()!).When(r => r.BasicAuthentication != null);
            RuleFor(r => r.JwtAuthentication).Null().When(r => r.BasicAuthentication != null).WithMessage("jwt authentication must be null when basic authentication has value");
            RuleFor(r => r.JwtAuthentication).SetValidator(new RestJobJwtAuthenticationValidator()!).When(r => r.JwtAuthentication != null);
            RuleFor(r => r.Proxy).SetValidator(new RestJobPropertiesProxyValidator()!).When(r => r.Proxy != null);

            RuleForEach(r => r.FormData)
                .Must(kvp => RestListKeyNotEmpty(kvp))
                .WithMessage("form data key is mandatory");

            RuleForEach(r => r.FormData)
                .Must(kvp => RestListKeyLength(kvp))
                .WithMessage("form data key maximum length is 100 chars");

            RuleForEach(r => r.FormData)
                .Must(kvp => RestListValueLength(kvp))
                .WithMessage("form data value maximum length is 1000 chars");

            RuleForEach(r => r.Headers)
                .Must(kvp => RestListKeyNotEmpty(kvp))
                .WithMessage("headers key is mandatory");

            RuleForEach(r => r.Headers)
                .Must(kvp => RestListKeyLength(kvp))
                .WithMessage("header key maximum length is 100 chars");

            RuleForEach(r => r.Headers)
                .Must(kvp => RestListValueLength(kvp))
                .WithMessage("header value maximum length is 1000 chars");
        }

        private async Task<bool> FilenameExists(RestJobProperties properties, string? filename, ValidationContext<RestJobProperties> context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filename)) { return true; }
            return await CommonValidations.FilenameExists(properties, "body file", filename, _cluster, context);
        }

        private static bool RestListKeyNotEmpty(KeyValuePair<string, string>? kvp)
        {
            return !string.IsNullOrEmpty(kvp?.Key);
        }

        private static bool RestListKeyLength(KeyValuePair<string, string>? kvp)
        {
            return kvp?.Key.Length <= 100;
        }

        private static bool RestListValueLength(KeyValuePair<string, string>? kvp)
        {
            return kvp?.Value?.Length <= 1000;
        }
    }
}