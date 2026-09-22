using FluentValidation;
using Planar.Service.General;
using System.Linq;

namespace Planar.Service.Validation;

public class ProcessJobPropertiesValidator : AbstractValidator<ProcessJobProperties>
{
    public ProcessJobPropertiesValidator(ClusterUtil cluster)
    {
        Include(new BaseProcessJobPropertiesValidator(cluster));

        RuleFor(e => e.Arguments)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of 'arguments' must be 1000 characters or fewer. You entered {e.Arguments?.Length ?? 0} characters");

        RuleFor(e => e.OutputEncoding)
            .Must(EncodingExists)
            .WithMessage("invalid 'output encoding' {PropertyValue}");

        RuleFor(e => e)
            .Must(ExitCodeValid);

        RuleFor(e => e.SuccessOutputRegex)
            .MaximumLength(500)
            .WithMessage(e => $"the length of 'success output regex' must be 500 characters or fewer. You entered {e.SuccessOutputRegex?.Length ?? 0} characters");

        RuleFor(e => e.FailOutputRegex)
            .MaximumLength(500);

        RuleFor(e => e.SuccessExitCodes)
            .Must(c => c == null || c.Count() <= 50)
            .WithMessage("'success exit codes' items count is more then maximum of 50");

        RuleFor(e => e.FailExitCodes)
            .Must(c => c == null || c.Count() <= 50)
            .WithMessage("'fail exit codes' items count is more then maximum of 50");
    }

    private static bool ExitCodeValid(ProcessJobProperties properties, ProcessJobProperties properties2, ValidationContext<ProcessJobProperties> context)
    {
        var counter = 0;
        if (properties.SuccessExitCodes != null && properties.SuccessExitCodes.Any()) { counter++; }
        if (properties.FailExitCodes != null && properties.FailExitCodes.Any()) { counter++; }
        if (!string.IsNullOrEmpty(properties.SuccessOutputRegex)) { counter++; }
        if (!string.IsNullOrEmpty(properties.FailOutputRegex)) { counter++; }

        if (counter > 1)
        {
            context.AddFailure("exit code", "only 1 of the following properties are allowed to be defined: 'success exit codes', 'success output regex', 'fail exit codes', 'fail output regex'");
            return false;
        }

        return true;
    }

    private static bool EncodingExists(ProcessJobProperties properties, string? outputEncoding, ValidationContext<ProcessJobProperties> context)
    {
        if (outputEncoding == null) { return true; }
        return CommonValidations.EncodingExists(outputEncoding, context);
    }
}