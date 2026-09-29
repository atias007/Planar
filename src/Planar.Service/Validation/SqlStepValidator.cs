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

        RuleFor(s => s.QueryResource)
            .NotEmpty()
            .WithMessage("'query resource' must not be empty")
            .Length(1, 100)
            .WithMessage(e => $"the length of 'query resource' must be between 1 and 100 characters. You entered {e.QueryResource?.Length ?? 0} characters");

        RuleFor(s => s.ConnectionName)
            .Length(3, 50)
            .WithMessage(e => $"the length of 'connection name' must be between 3 and 50 characters. You entered {e.ConnectionName?.Length ?? 0} characters");

        RuleFor(j => j.ConnectionName).Must(ValidateGlobalConfigExists);

        RuleFor(s => s.EffectedRowsSource)
            .Must(e => Enum.TryParse<EffectedRowsSourceMembers>(e, ignoreCase: true, out _))
            .WithMessage("'effected rows source' with value '{PropertyValue}' must have value from the following list: " + string.Join(", ", Enum.GetNames<EffectedRowsSourceMembers>()));
    }

    private static bool ValidateGlobalConfigExists(SqlStep step, string? value, ValidationContext<SqlStep> context)
    {
        var result = CommonValidations.GlobalConfigExists("default connection name", value, context);
        return result;
    }
}