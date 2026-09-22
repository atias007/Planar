using FluentValidation;
using System;

namespace Planar.Service.Validation;

public class SqlStepValidator : AbstractValidator<SqlStep>
{
    public SqlStepValidator()
    {
        RuleFor(s => s.Name)
            .NotEmpty()
            .WithMessage("'name' must not be empty")
            .Length(1, 50)
            .WithMessage(e => $"the length of 'name' must be between 1 and 50 characters. You entered {e.Name?.Length ?? 0} characters");

        RuleFor(s => s.Filename)
            .NotEmpty()
            .WithMessage("'filename' must not be empty")
            .Length(1, 100)
            .WithMessage(e => $"the length of 'filename' must be between 1 and 100 characters. You entered {e.Filename?.Length ?? 0} characters");

        RuleFor(s => s.ConnectionName)
            .Length(1, 50)
            .WithMessage(e => $"the length of 'connection name' must be between 1 and 50 characters. You entered {e.ConnectionName?.Length ?? 0} characters");

        RuleFor(s => s.EffectedRowsSource)
            .Must(e => Enum.TryParse<EffectedRowsSourceMembers>(e, ignoreCase: true, out _))
            .WithMessage("'effected rows source' with value '{PropertyValue}' must have value from the following list: " + string.Join(", ", Enum.GetNames<EffectedRowsSourceMembers>()));
    }
}