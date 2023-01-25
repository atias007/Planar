using FluentValidation;
using Planar.Service.General;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class ProcessJobPropertiesValidator : AbstractValidator<ProcessJobProperties>
    {
        private readonly ClusterUtil _cluster;

        public ProcessJobPropertiesValidator(ClusterUtil cluster)
        {
            _cluster = cluster;
            RuleFor(e => e.Path).NotEmpty().MaximumLength(500);
            RuleFor(e => e.Path).MustAsync(PathExists).When(e => !string.IsNullOrEmpty(e.Path));
            RuleFor(e => e.Filename).NotEmpty().MaximumLength(500);
            RuleFor(e => e.Filename).MustAsync(FilenameExists).When(e => !string.IsNullOrEmpty(e.Path) && !string.IsNullOrEmpty(e.Filename));
            RuleFor(e => e.Arguments).MaximumLength(1000);
            RuleFor(e => e.OutputEncoding).Must(EncodingExists);
            RuleFor(e => e).Must(ExitCodeValid);
            RuleFor(e => e.SuccessOutputPattern).MaximumLength(500);
            RuleFor(e => e.FailOutputPattern).MaximumLength(500);
            RuleFor(e => e.SuccessExitCodes).Must(c => c == null || c.Count() <= 50).WithMessage("{PropertyName} items count is more then maximum of 50");
            RuleFor(e => e.FailExitCodes).Must(c => c == null || c.Count() <= 50).WithMessage("{PropertyName} items count is more then maximum of 50");
        }

        private bool ExitCodeValid(ProcessJobProperties properties, ProcessJobProperties properties2, ValidationContext<ProcessJobProperties> context)
        {
            var counter = 0;
            if (properties.SuccessExitCodes != null && properties.SuccessExitCodes.Any()) { counter++; }
            if (properties.FailExitCodes != null && properties.FailExitCodes.Any()) { counter++; }
            if (!string.IsNullOrEmpty(properties.SuccessOutputPattern)) { counter++; }
            if (!string.IsNullOrEmpty(properties.FailOutputPattern)) { counter++; }

            if (counter > 1)
            {
                context.AddFailure("exit code", "only 1 of the following properties are allowed to be defined: success exit codes, success output pattern, fail exit codes, fail output pattern");
                return false;
            }

            return true;
        }

        private bool EncodingExists(ProcessJobProperties properties, string outputEncoding, ValidationContext<ProcessJobProperties> context)
        {
            return CommonValidations.EncodingExists(outputEncoding, context);
        }

        private async Task<bool> PathExists(ProcessJobProperties properties, string path, ValidationContext<ProcessJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.PathExists(path, _cluster, context);
        }

        private async Task<bool> FilenameExists(ProcessJobProperties properties, string filename, ValidationContext<ProcessJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.FilenameExists(properties, filename, _cluster, context);
        }
    }
}