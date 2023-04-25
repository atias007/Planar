using FluentValidation;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Validation
{
    public class GlobalConfigDataValidator : AbstractValidator<GlobalConfig>
    {
        private static readonly IEnumerable<string> _types = Enum.GetNames<GlobalConfigTypes>().Select(n => n.ToLower());

        public GlobalConfigDataValidator()
        {
            RuleFor(f => f.Key).NotEmpty().MaximumLength(100);
            RuleFor(f => f.Value).MaximumLength(4000);
            RuleFor(f => f.Type).NotEmpty().MaximumLength(10)
                .Must(IsValidType)
                .WithMessage("{PropertyName} has invalid value '{PropertyValue}'. valid values are: " + string.Join(',', _types));

            RuleFor(f => f.Value)
                .NotEmpty()
                .When(f => string.Equals(f.Type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase))
                .WithMessage(f => "{PropertyName} is required when config type is " + f.Type.ToLower());

            RuleFor(f => f.Value)
                .Must(f => ValidationUtil.IsYmlValid(f))
                .When(f => string.Equals(f.Type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase))
                .WithMessage(f => "{PropertyName} has invalid yml format");

            RuleFor(f => f.Value)
                .Must(f => ValidationUtil.IsJsonValid(f))
                .When(f => string.Equals(f.Type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase))
                .WithMessage(f => "{PropertyName} has invalid json format");
        }

        private static bool IsValidType(string type)
        {
            if (type == null) { return false; }
            return _types.Contains(type.ToLower());
        }
    }
}