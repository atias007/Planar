using FluentValidation;
using Planar.Service.Model;
using System.Linq;

namespace Planar.Service.Validation
{
    public class GlobalConfigDataValidator : AbstractValidator<GlobalConfig>
    {
        private static readonly string[] _types = new[] { "json", "yml", "string" };

        public GlobalConfigDataValidator()
        {
            RuleFor(f => f.Key).NotEmpty().MaximumLength(50);
            RuleFor(f => f.Value).NotEmpty().MaximumLength(1000);
            RuleFor(f => f.Type).NotEmpty().MaximumLength(10)
                .Must(IsValidType)
                .WithMessage("{PropertyName} has invalid value {PropertyValue}. valid values are: " + string.Join(',', _types));
        }

        private static bool IsValidType(string type)
        {
            if (type == null) { return false; }
            return _types.Contains(type.ToLower());
        }
    }
}