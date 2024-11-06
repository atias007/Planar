using FluentValidation;
using Planar.Service.General;
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

            RuleFor(e => e.Path).MustAsync(PathExists)
                .When(e => !string.IsNullOrEmpty(e.Path));

            RuleFor(j => j.DefaultConnectionName).Length(1, 50);
            RuleFor(j => j.DefaultConnectionName)
                .NotEmpty()
                .When(j => j.Steps != null && j.Steps.Exists(s => string.IsNullOrWhiteSpace(s.ConnectionName)))
                .WithMessage("{PropertyName} must have value when any step has no connection name");

            RuleFor(s => s.Transaction)
                .Equal(false)
                .When(p => p.Steps != null && p.Steps.Exists(s => !string.IsNullOrWhiteSpace(s.ConnectionName)))
                .WithMessage("{PropertyName} must be false when there is a step with specific connection name. Transaction only aloowed with single default connection name");

            RuleFor(j => j.TransactionIsolationLevel).IsInEnum();
            RuleFor(j => j.ContinueOnError)
                .Equal(false)
                .When(j => j.Transaction)
                .WithMessage("{PropertyName} must be false when transaction is true");

            RuleFor(j => j.Steps).NotEmpty();
            RuleForEach(j => j.Steps).SetValidator((a, b) => new SqlStepValidator());
            RuleForEach(j => j.Steps)
                .Must(s => !string.IsNullOrWhiteSpace(s.ConnectionName))
                .When(p => string.IsNullOrWhiteSpace(p.DefaultConnectionName))
                .WithMessage("connection name property for step must have value when no default connection name");
            RuleForEach(j => j.Steps).MustAsync(FilenameExists);
        }

        private async Task<bool> PathExists(SqlJobProperties properties, string? path, ValidationContext<SqlJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.PathExists(path, _cluster, context);
        }

        private async Task<bool> FilenameExists(SqlJobProperties properties, SqlStep step, ValidationContext<SqlJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.FilenameExists(properties, "filename", step.Filename, _cluster, context);
        }
    }
}