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

        RuleFor(s => s.ConnectionName).Length(1, 50);
        RuleFor(s => s.Filename).NotEmpty().Length(1, 100);
        RuleFor(s => s.Filename).MustAsync(FilenameExists);
    }

    private async Task<bool> FilenameExists(SqlTableReportJobProperties properties, string filename, ValidationContext<SqlTableReportJobProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.FilenameExists(properties, "filename", filename, _cluster, context);
    }
}