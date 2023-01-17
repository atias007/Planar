using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Planar.Service.General;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public class PlanarJobPropertiesValidator : AbstractValidator<PlanarJobProperties>
    {
        private readonly ClusterUtil _cluster;

        public PlanarJobPropertiesValidator(ClusterUtil cluster)
        {
            _cluster = cluster;
            RuleFor(e => e.Path).NotEmpty().MaximumLength(500);
            RuleFor(e => e.Path).MustAsync(PathExists).When(e => !string.IsNullOrEmpty(e.Path));
            RuleFor(e => e.Filename).NotEmpty().MaximumLength(500);
            RuleFor(e => e.Filename).MustAsync(FilenameExists).When(e => !string.IsNullOrEmpty(e.Path) && !string.IsNullOrEmpty(e.Filename));
            RuleFor(e => e.ClassName).NotEmpty().MaximumLength(500);
        }

        private async Task<bool> PathExists(PlanarJobProperties properties, string path, ValidationContext<PlanarJobProperties> context, CancellationToken cancellationToken = default)
        {
            try
            {
                ServiceUtil.ValidateJobFolderExists(path);
                await _cluster.ValidateJobFolderExists(path);
                return true;
            }
            catch (PlanarException ex)
            {
                context.AddFailure(ex.Message);
                return false;
            }
        }

        private async Task<bool> FilenameExists(PlanarJobProperties properties, string filename, ValidationContext<PlanarJobProperties> context, CancellationToken cancellationToken = default)
        {
            try
            {
                ServiceUtil.ValidateJobFileExists(properties.Path, filename);
                await _cluster.ValidateJobFileExists(properties.Path, filename);
                return true;
            }
            catch (PlanarException ex)
            {
                context.AddFailure(ex.Message);
                return false;
            }
        }
    }
}