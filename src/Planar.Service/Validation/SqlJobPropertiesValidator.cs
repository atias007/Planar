using FluentValidation;
using Planar.Service.General;
using System.Collections.Generic;
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

            RuleFor(s => s.Path).NotEmpty().Length(1, 1000);
            RuleFor(j => j.DefaultConnectionName).Length(1, 50);
            RuleFor(j => j.DefaultConnectionName)
                .NotEmpty()
                .When(j => j.Steps != null && j.Steps.Any(s => string.IsNullOrWhiteSpace(s.ConnectionName)))
                .WithMessage("{PropertyName} must have value when any step has no connection name");

            RuleFor(s => s.Transaction)
                .Equal(false)
                .When(p => p.Steps != null && p.Steps.Any(s => !string.IsNullOrWhiteSpace(s.ConnectionName)))
                .WithMessage("{PropertyName} must be false when there is a step with specific connection name. Transaction only aloowed with single default connection name");

            RuleFor(j => j.TransactionIsolationLevel).IsInEnum();
            RuleFor(j => j.ContinueOnError)
                .Equal(false)
                .When(j => j.Transaction)
                .WithMessage("{PropertyName} must be false when transaction is true");

            RuleFor(j => j.ConnectionStrings)
                .Must(NoDuplicates)
                .WithMessage("{PropertyName} name must be unique. there are duplicate in name property of {PropertyName}");

            RuleForEach(j => j.ConnectionStrings)
                .SetValidator(new SqlConnectionStringValidator());

            RuleFor(j => j.Steps).NotEmpty();
            RuleForEach(j => j.Steps).SetValidator((a, b) => new SqlStepValidator());
            RuleForEach(j => j.Steps)
                .Must(s => !string.IsNullOrWhiteSpace(s.ConnectionName))
                .When(p => string.IsNullOrWhiteSpace(p.DefaultConnectionName))
                .WithMessage("connection name property for step must have value when no default connection name");
            RuleForEach(j => j.Steps).MustAsync(FilenameExists);
        }

        private bool NoDuplicates(List<SqlConnectionString>? list)
        {
            if (list == null || list.Count == 0) { return true; }
            var names = list.Select(l => l.Name).ToList();
            var origin = names.Count;
            var check = names.Distinct().Count();
            return origin == check;
        }

        private async Task<bool> FilenameExists(SqlJobProperties properties, SqlStep step, ValidationContext<SqlJobProperties> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.FilenameExists(properties, "filename", step.Filename, _cluster, context);
        }
    }
}