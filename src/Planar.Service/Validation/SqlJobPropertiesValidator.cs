using FluentValidation;
using Planar.Service.General;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class SqlJobPropertiesValidator : AbstractValidator<SqlJobProperties>
{
    private readonly ClusterUtil _cluster;

    public SqlJobPropertiesValidator(ClusterUtil cluster)
    {
        _cluster = cluster;

        RuleFor(s => s.Path)
            .NotEmpty()
            .WithMessage("'path' must not be empty");

        RuleFor(j => j.DefaultConnectionName)
            .Length(1, 50)
            .WithMessage(e => $"the length of 'default connection name' must be between 1 and 50 characters. You entered {e.DefaultConnectionName?.Length ?? 0} characters");

        RuleFor(j => j.DefaultConnectionName)
            .NotEmpty()
            .When(j => j.Steps != null && j.Steps.Exists(s => string.IsNullOrWhiteSpace(s.ConnectionName)))
            .WithMessage("'default connection name' must have value when any step has no connection name");

        RuleFor(s => s.Transaction)
            .Equal(false)
            .When(p => p.Steps != null && p.Steps.Exists(s => !string.IsNullOrWhiteSpace(s.ConnectionName)))
            .WithMessage("'transaction' must be false when there is a step with specific connection name. Transaction only allowed with single default connection name");

        RuleFor(j => j.TransactionIsolationLevel)
            .IsInEnum()
            .WithMessage(e => $"'transaction isolation level' with value '{e.TransactionIsolationLevel}' is not valid. Available options are: {string.Join(", ", Enum.GetNames(typeof(System.Data.IsolationLevel)))}");

        RuleFor(j => j.ContinueOnError)
            .Equal(false)
            .When(j => j.Transaction)
            .WithMessage("'continue on error' must be false when transaction is true");

        RuleFor(j => j.Steps)
            .NotEmpty()
            .WithMessage("'steps' must not be empty");

        RuleForEach(j => j.Steps).SetValidator((a, b) => new SqlStepValidator());

        RuleForEach(j => j.Steps)
            .Must(s => !string.IsNullOrWhiteSpace(s.ConnectionName))
            .When(p => string.IsNullOrWhiteSpace(p.DefaultConnectionName))
            .WithMessage("'connection name' on any step must have value when no 'default connection name' defined");

        RuleForEach(j => j.Steps).MustAsync(FilenameExists);
    }

    private async Task<bool> FilenameExists(SqlJobProperties properties, SqlStep step, ValidationContext<SqlJobProperties> context, CancellationToken cancellationToken = default)
    {
        var fullFilename = Path.Combine(properties.Path, step.Filename ?? string.Empty);
        return await CommonValidations.FilenameExists("filename", fullFilename, _cluster, context);
    }
}