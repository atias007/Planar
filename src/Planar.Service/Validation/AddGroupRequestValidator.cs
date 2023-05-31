using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class AddGroupRequestValidator : AbstractValidator<AddGroupRequest>
    {
        public AddGroupRequestValidator()
        {
            RuleFor(g => g.Name).NotEmpty().Length(2, 50);
            RuleFor(u => u.AdditionalField1).MaximumLength(500);
            RuleFor(u => u.AdditionalField2).MaximumLength(500);
            RuleFor(u => u.AdditionalField3).MaximumLength(500);
            RuleFor(u => u.AdditionalField4).MaximumLength(500);
            RuleFor(u => u.AdditionalField5).MaximumLength(500);
            RuleFor(u => u.Role).Must(r =>
            {
                if (string.IsNullOrEmpty(r)) { return true; }
                return Enum.TryParse<Roles>(r, ignoreCase: true, out _);
            })
            .WithMessage("role '{PropertyValue}' is not supported");
        }
    }
}