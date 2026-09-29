using FluentValidation;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class SqlTableReportJobPropertiesValidator : AbstractValidator<SqlTableReportJobProperties>
{
    private readonly IServiceScopeFactory scopeFactory;

    public SqlTableReportJobPropertiesValidator(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;

        RuleFor(s => s.ConnectionName)
            .Length(3, 50)
            .WithMessage(e => $"the length of 'connection name' must be between 3 and 50 characters. You entered {e.ConnectionName?.Length ?? 0} characters");

        RuleFor(s => s.ConnectionName).Must(ValidateGlobalConfigExists);

        RuleFor(s => s.QueryResource)
            .NotEmpty()
            .WithMessage("'query resource' must not be empty")
            .Length(1, 100)
            .WithMessage(e => $"the length of 'query resource' must be between 1 and 100 characters. You entered {e.QueryResource?.Length ?? 0} characters");

        RuleFor(s => s.QueryResource).MustAsync(ValidateResourceExists);

        RuleFor(s => s.Group)
            .Length(2, 50)
            .WithMessage(e => $"the length of 'group' must be between 2 and 50 characters. You entered {e.Group?.Length ?? 0} characters");

        RuleFor(s => s.Group).MustAsync(ValidateGroupExists);

        RuleFor(s => s.Title)
            .NotEmpty()
            .WithMessage("'title' must not be empty")
            .Length(1, 100)
            .WithMessage(e => $"the length of 'title' must be between 1 and 100 characters. You entered {e.Title?.Length ?? 0} characters");

        RuleFor(s => s.Timeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(5))
            .WithMessage("'timeout' must be greater than or equal to 5 seconds");

        RuleFor(s => s.Timeout)
            .LessThanOrEqualTo(TimeSpan.FromHours(2))
            .WithMessage("'timeout' must be less than or equal to 2 hours");
    }

    private async Task<bool> ValidateResourceExists(SqlTableReportJobProperties properties, string value, ValidationContext<SqlTableReportJobProperties> context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value)) { return true; }
        await using var scope = scopeFactory.CreateAsyncScope();
        var dal = scope.ServiceProvider.GetRequiredService<IResourceData>();
        return await CommonValidations.ResourceExists("query resource", value, dal, context);
    }

    private static bool ValidateGlobalConfigExists(SqlTableReportJobProperties properties, string? value, ValidationContext<SqlTableReportJobProperties> context)
    {
        var result = CommonValidations.GlobalConfigExists("default connection name", value, context);
        return result;
    }

    private async Task<bool> ValidateGroupExists(SqlTableReportJobProperties properties, string value, ValidationContext<SqlTableReportJobProperties> context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value)) { return true; }
        await using var scope = scopeFactory.CreateAsyncScope();
        var dal = scope.ServiceProvider.GetRequiredService<IGroupData>();
        var result = await dal.IsGroupNameExists(value);
        if (!result)
        {
            context.AddFailure("group", $"group '{value}', does not exist");
        }

        return result;
    }
}