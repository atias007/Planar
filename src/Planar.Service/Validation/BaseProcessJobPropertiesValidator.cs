using FluentValidation;
using Planar.Service.General;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class BaseProcessJobPropertiesValidator : AbstractValidator<BaseProcessJobProperties>
{
    private readonly ClusterUtil _cluster;

    public BaseProcessJobPropertiesValidator(ClusterUtil cluster)
    {
        _cluster = cluster;

        RuleFor(e => e.Filename).NotEmpty().MaximumLength(500);
        RuleFor(e => e.Filename).MustAsync(FilenameExists).When(e => !string.IsNullOrEmpty(e.Filename));

        RuleFor(e => e.Filename).Must(FileExtentionIsExe)
            .When(e => !string.IsNullOrEmpty(e.Filename))
            .WithMessage("property '{PropertyName}' with value '{PropertyValue}' must have 'exe' extention");

        RuleFor(e => e.Domain).MaximumLength(100);
        RuleFor(e => e.UserName).MaximumLength(100);
        RuleFor(e => e.Password).MaximumLength(100);

        RuleFor(e => e.Domain).Null()
            .When(p => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            .WithMessage("{PropertyName} must be null when operation system is not windows");

        RuleFor(e => e.Password).Null()
            .When(p => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            .WithMessage("{PropertyName} must be null when operation system is not windows");
    }

    private async Task<bool> FilenameExists(BaseProcessJobProperties properties, string? filename, ValidationContext<BaseProcessJobProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.FilenameExists("filename", filename, _cluster, context);
    }

    private static bool FileExtentionIsExe(string filename)
    {
        const string exe = ".exe";
        var fi = new FileInfo(filename);
        return string.Equals(fi.Extension, exe);
    }
}