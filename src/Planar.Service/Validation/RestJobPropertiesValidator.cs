using FluentValidation;
using RestJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class RestJobPropertiesValidator : AbstractValidator<RestJobProperties>
    {
        private static readonly string[] _methods = new[] { "POST", "GET", "PUT", "DELETE", "PATCH", "HEAD" };

        public RestJobPropertiesValidator()
        {
            RuleFor(r => r.Url)
                .NotEmpty()
                .MaximumLength(1000)
                .Must(r => Uri.TryCreate(r, UriKind.Absolute, out _))
                .WithMessage("'{PropertyValue}' is not valid url");

            RuleFor(r => r.Method)
                .NotEmpty()
                .Must(r => _methods.Any(m => string.Equals(r, m, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("methot with value '{PropertyValue}' is invalid. avaliable options are: " + string.Join(',', _methods));
        }
    }
}