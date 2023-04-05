using FluentValidation;
using Planar.Service.General;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class SqlJobPropertiesValidator : AbstractValidator<SqlJobProperties>
    {
        private readonly ClusterUtil _cluster;

        public SqlJobPropertiesValidator(ClusterUtil cluster)
        {
            _cluster = cluster;

            RuleFor(j => j.IsolationLevel).IsInEnum();
            RuleFor(j => j.ConnectionString)
                .NotEmpty()
                .When(j => j.Steps != null && j.Steps.Any(s => string.IsNullOrWhiteSpace(s.ConnectionString)));
            RuleFor(s => s.Path).NotEmpty().Length(1, 1000);
            RuleFor(s => s.Transaction)
                .Equal(false)
                .When(p => p.Steps != null && p.Steps.Any(s => !string.IsNullOrWhiteSpace(s.ConnectionString)))
                .WithMessage("{PropertyName} must be false when there is step with specific connection string");
            RuleFor(j => j.Steps).NotEmpty();
            RuleFor(j => j.ContinueOnError)
                .Equal(false)
                .When(j => j.Transaction)
                .WithMessage("{PropertyName} must be false when transaction is true");
            RuleForEach(j => j.Steps).SetValidator((a, b) => new SqlStepValidator());
            RuleForEach(j => j.Steps)
                .Must(s => !string.IsNullOrWhiteSpace(s.ConnectionString))
                .When(p => string.IsNullOrWhiteSpace(p.ConnectionString))
                .WithMessage("connection string property for sql job step must have value when no default connection string");
            RuleForEach(j => j.Steps).MustAsync(FilenameExists);
        }

        private async Task<bool> FilenameExists(SqlJobProperties properties, SqlStep step, ValidationContext<SqlJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.FilenameExists(properties, step.Filename, _cluster, context);
        }
    }
}