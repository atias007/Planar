using FluentValidation;
using Planar.Service.General;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public class PlanarJobPropertiesValidator : AbstractValidator<PlanarJobProperties>
{
    private static readonly string[] AllowedInvokeMethods = ["process", "http", "redis", "rabbitmq"];

    public PlanarJobPropertiesValidator(ClusterUtil cluster)
    {
        RuleFor(e => e.InvokeMethod)
            .NotEmpty()
            .Must(value => AllowedInvokeMethods.Any(method => string.Equals(method, value, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"invoke method must be one of: {string.Join(", ", AllowedInvokeMethods)}");

        RuleFor(e => e.Process)
            .SetValidator(new PlanarJobProcessPropertiesValidator(cluster)!)
            .When(e => e.Process != null);

        RuleFor(e => e.Http)
            .SetValidator(new PlanarJobHttpPropertiesValidator()!)
            .When(e => e.Http != null);

        RuleFor(e => e.Redis)
            .SetValidator(new PlanarJobRedisPropertiesValidator()!)
            .When(e => e.Redis != null);

        RuleFor(e => e.RabbitMq)
            .SetValidator(new PlanarJobRabbitMqPropertiesValidator()!)
            .When(e => e.RabbitMq != null);

        RuleFor(e => e).Must(e =>
        {
            var count = 0;
            if (e.Process != null) { count++; }
            if (e.Http != null) { count++; }
            if (e.Redis != null) { count++; }
            if (e.RabbitMq != null) { count++; }
            return count == 1;
        }).WithMessage("exactly one of 'process', 'http', 'redis' or 'rabbitmq' properties must be provided");

        RuleFor(e => e).Must(e =>
        {
            if (string.Equals(e.InvokeMethod, "process", StringComparison.OrdinalIgnoreCase))
            {
                return e.Process != null;
            }
            else if (string.Equals(e.InvokeMethod, "http", StringComparison.OrdinalIgnoreCase))
            {
                return e.Http != null;
            }
            else if (string.Equals(e.InvokeMethod, "redis", StringComparison.OrdinalIgnoreCase))
            {
                return e.Redis != null;
            }
            else if (string.Equals(e.InvokeMethod, "rabbitmq", StringComparison.OrdinalIgnoreCase))
            {
                return e.RabbitMq != null;
            }
            return false;
        }).WithMessage("the property corresponding to the invoke method must be provided");
    }
}

public class PlanarJobRabbitMqPropertiesValidator : AbstractValidator<PlanarJobRabbitMqProperties>
{
    public PlanarJobRabbitMqPropertiesValidator()
    {
        RuleFor(e => e.Exchange).NotEmpty().MaximumLength(100);
        RuleFor(e => e.RoutingKey).NotEmpty().MaximumLength(100);
    }
}

public class PlanarJobRedisPropertiesValidator : AbstractValidator<PlanarJobRedisProperties>
{
    public PlanarJobRedisPropertiesValidator()
    {
        RuleFor(e => e.StreamName).NotEmpty().MaximumLength(100);
        RuleFor(e => e.ConsumerGroup).NotEmpty().MaximumLength(100);
    }
}

public class PlanarJobHttpPropertiesValidator : AbstractValidator<PlanarJobHttpProperties>
{
    public PlanarJobHttpPropertiesValidator()
    {
        RuleFor(e => e.Url).NotEmpty().MaximumLength(1000).IsUri();
    }
}

public class PlanarJobProcessPropertiesValidator : AbstractValidator<PlanarJobProcessProperties>
{
    private readonly ClusterUtil _cluster;

    public PlanarJobProcessPropertiesValidator(ClusterUtil cluster)
    {
        _cluster = cluster;
        Include(new BaseProcessJobPropertiesValidator());
        RuleFor(e => e.Path).MustAsync(PathExists)
            .When(e => !string.IsNullOrEmpty(e.Path));

        RuleFor(e => e.Filename).NotEmpty().MaximumLength(500);
        RuleFor(e => e.Filename).MustAsync(FilenameExists)
            .When(e => !string.IsNullOrEmpty(e.Path) && !string.IsNullOrEmpty(e.Filename));

        RuleFor(e => e.Filename).Must(FileExtentionIsExe)
            .When(e => !string.IsNullOrEmpty(e.Filename))
            .WithMessage("property '{PropertyName}' with value '{PropertyValue}' must have 'exe' extention");
    }

    private async Task<bool> PathExists(PlanarJobProcessProperties properties, string? path, ValidationContext<PlanarJobProcessProperties> context, CancellationToken cancellationToken = default)
    {
        return await CommonValidations.PathExists(path, _cluster, context);
    }

    private async Task<bool> FilenameExists(PlanarJobProcessProperties properties, string? filename, ValidationContext<PlanarJobProcessProperties> context, CancellationToken cancellationToken = default)
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