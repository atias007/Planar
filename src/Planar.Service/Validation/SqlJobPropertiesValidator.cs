using FluentValidation;
using Planar.Service.General;
using System.Linq;

namespace Planar.Service.Validation
{
    public class SqlJobPropertiesValidator : AbstractValidator<SqlJobProperties>
    {
        public SqlJobPropertiesValidator(ClusterUtil cluster)
        {
            RuleFor(j => j.IsolationLevel).IsInEnum();
            RuleFor(j => j.ConnectionString).NotEmpty()
                .When(j => j.Steps != null && j.Steps.Any(s => string.IsNullOrWhiteSpace(s.ConnectionString)));
            RuleFor(s => s.Path).NotEmpty().Length(1, 1000);
            RuleFor(s => s.Transaction).Must(t => false)
                .When(p => p.Steps != null && p.Steps.Any(s => !string.IsNullOrWhiteSpace(s.ConnectionString)))
                .WithMessage("{PropertyName} must be false when there is step with specific connection string");
            RuleFor(j => j.Steps).NotEmpty();
            RuleForEach(j => j.Steps).SetValidator((a, b) => new SqlStepValidator(cluster));
            RuleForEach(j => j.Steps).Must(s => !string.IsNullOrWhiteSpace(s.ConnectionString))
                .When(p => string.IsNullOrWhiteSpace(p.ConnectionString))
                .WithMessage("connection string property for sql job step must have value when no default connection string");
        }
    }
}