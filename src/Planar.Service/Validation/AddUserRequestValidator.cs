using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Validation;

namespace Planar.Service
{
    public class AddUserRequestValidator : AbstractValidator<AddUserRequest>
    {
        public AddUserRequestValidator()
        {
            RuleFor(r => r.Username).NotEmpty().Length(2, 50);
            RuleFor(r => r.FirstName).NotEmpty().Length(2, 50);
            RuleFor(r => r.LastName).Length(2, 50);
            RuleFor(r => r.EmailAddress1).Length(5, 250).EmailAddress();
            RuleFor(r => r.EmailAddress2).Length(5, 250).EmailAddress();
            RuleFor(r => r.EmailAddress3).Length(5, 250).EmailAddress();
            RuleFor(r => r.PhoneNumber1).Length(9, 50).OnlyDigits();
            RuleFor(r => r.PhoneNumber2).Length(9, 50).OnlyDigits();
            RuleFor(r => r.PhoneNumber3).Length(9, 50).OnlyDigits();
            RuleFor(u => u.AdditionalField1).MaximumLength(500);
            RuleFor(u => u.AdditionalField2).MaximumLength(500);
            RuleFor(u => u.AdditionalField3).MaximumLength(500);
            RuleFor(u => u.AdditionalField4).MaximumLength(500);
            RuleFor(u => u.AdditionalField5).MaximumLength(500);
        }
    }
}