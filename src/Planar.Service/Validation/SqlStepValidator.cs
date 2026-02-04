using FluentValidation;
using System;

namespace Planar.Service.Validation;

public class SqlStepValidator : AbstractValidator<SqlStep>
{
    public SqlStepValidator()
    {
        RuleFor(s => s.Name).NotEmpty().Length(1, 50);
        RuleFor(s => s.Filename).NotEmpty().Length(1, 100);
        RuleFor(s => s.ConnectionName).Length(1, 50);
        RuleFor(s => s.EffectedRowsSource)
            .Must(e => Enum.TryParse<EffectedRowsSourceMembers>(e, ignoreCase: true, out _))
            .WithMessage("{PropertyName} with value '{PropertyValue}' must have value from the following list: " + string.Join(", ", Enum.GetNames<EffectedRowsSourceMembers>()));
    }
}