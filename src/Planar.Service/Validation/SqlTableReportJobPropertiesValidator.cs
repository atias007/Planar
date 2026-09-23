using FluentValidation;
using Planar.Service.General;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class SqlTableReportJobPropertiesValidator : AbstractValidator<SqlTableReportJobProperties>
{
    private readonly ClusterUtil _cluster;

    public SqlTableReportJobPropertiesValidator(ClusterUtil cluster)
    {
        _cluster = cluster;

        RuleFor(s => s.ConnectionName)
            .Length(1, 50)
            .WithMessage(e => $"the length of 'connection name' must be between 1 and 50 characters. You entered {e.ConnectionName?.Length ?? 0} characters");
        RuleFor(s => s.Filename)
            .NotEmpty()
            .WithMessage("'filename' must not be empty")
            .Length(1, 100)
            .WithMessage(e => $"the length of 'filename' must be between 1 and 100 characters. You entered {e.Filename?.Length ?? 0} characters");
        RuleFor(s => s.Filename).MustAsync(FilenameExists);
    }

    private async Task<bool> FilenameExists(SqlTableReportJobProperties properties, string filename, ValidationContext<SqlTableReportJobProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.FilenameExists("filename", filename, _cluster, context);
    }
}