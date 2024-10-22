using FluentValidation;
using Planar.Service.General;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class PlanarJobPropertiesValidator : AbstractValidator<PlanarJobProperties>
{
    private readonly ClusterUtil _cluster;

    public PlanarJobPropertiesValidator(ClusterUtil cluster)
    {
        _cluster = cluster;
        Include(new BaseProcessJobPropertiesValidator());
        RuleFor(e => e.Path).NotEmpty().MaximumLength(500);
        RuleFor(e => e.Path).MustAsync(PathExists)
            .When(e => !string.IsNullOrEmpty(e.Path));

        RuleFor(e => e.Filename).NotEmpty().MaximumLength(500);
        RuleFor(e => e.Filename).MustAsync(FilenameExists)
            .When(e => !string.IsNullOrEmpty(e.Path) && !string.IsNullOrEmpty(e.Filename));

        RuleFor(e => e.Filename).Must(FileExtentionIsExe)
            .When(e => !string.IsNullOrEmpty(e.Filename))
            .WithMessage("property '{PropertyName}' with value '{PropertyValue}' must have 'exe' extention");
    }

    private async Task<bool> PathExists(PlanarJobProperties properties, string? path, ValidationContext<PlanarJobProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.PathExists(path, _cluster, context);
    }

    private async Task<bool> FilenameExists(PlanarJobProperties properties, string? filename, ValidationContext<PlanarJobProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.FilenameExists(properties, "filename", filename, _cluster, context);
    }

    private static bool FileExtentionIsExe(string filename)
    {
        const string exe = ".exe";
        var fi = new FileInfo(filename);
        return string.Equals(fi.Extension, exe);
    }
}