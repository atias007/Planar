using FluentValidation;
using Planar.Service.General;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class SqlStepValidator : AbstractValidator<SqlStep>
    {
        private readonly ClusterUtil _cluster;

        public SqlStepValidator(ClusterUtil cluster)
        {
            _cluster = cluster;
            RuleFor(s => s.Name).NotEmpty().Length(1, 50);
            RuleFor(s => s.Filename).NotEmpty().Length(1, 100).MustAsync(FilenameExists);
        }

        private async Task<bool> FilenameExists(SqlStep properties, string? filename, ValidationContext<SqlStep> context, CancellationToken cancellationToken = default)
        {
            return await CommonValidations.FilenameExists(filename, _cluster, context);
        }
    }
}