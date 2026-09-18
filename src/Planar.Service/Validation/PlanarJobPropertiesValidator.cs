using FluentValidation;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Planar.Common;
using Planar.Service.General;
using System;
using System.Linq;

namespace Planar.Service.Validation;

public class PlanarJobPropertiesValidator : AbstractValidator<PlanarJobProperties>
{
    private static readonly string[] AllowedInvokeMethods = ["process", "http", "redis", "rabbitmq"];

    public PlanarJobPropertiesValidator(ClusterUtil cluster)
    {
        RuleFor(e => e.InvokeMethod)
            .NotEmpty()
            .WithMessage("'invoke method' must not be empty");

        RuleFor(e => e.InvokeMethod)
            .Must(value => AllowedInvokeMethods.Any(method => string.Equals(method, value, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"'invoke method' must be one of: {string.Join(", ", AllowedInvokeMethods)}");

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        RuleFor(e => e.Process)
            .SetValidator(new PlanarJobProcessPropertiesValidator(cluster))
            .When(e => e.Process != null);

        RuleFor(e => e.Http)
            .SetValidator(new PlanarJobHttpPropertiesValidator())
            .When(e => e.Http != null);

        RuleFor(e => e.Redis)
            .SetValidator(new PlanarJobRedisPropertiesValidator())
            .When(e => e.Redis != null);

        RuleFor(e => e.RabbitMq)
            .SetValidator(new PlanarJobRabbitMqPropertiesValidator())
            .When(e => e.RabbitMq != null);

        RuleFor(e => e.EncryptPayload)
            .Equal(false)
            .When(_ => AppSettings.General.EncryptionKeyBytes == null)
            .WithMessage("'encrypt payload' is not supported when planar encryption key is not set. (general section at appsettings.yml)");

#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        RuleFor(e => e).Must(e =>
        {
            var count = 0;
            if (e.Process != null) { count++; }
            if (e.Http != null) { count++; }
            if (e.Redis != null) { count++; }
            if (e.RabbitMq != null) { count++; }
            return count == 1;
        }).WithMessage("exactly one of 'process', 'http', 'redis' or 'rabbitmq' properties must be provided");

        RuleFor(e => e)
            .Must(e => e.Process != null)
            .When(e => string.Equals(e.InvokeMethod, "process", StringComparison.OrdinalIgnoreCase))
            .WithMessage("'process' is null or empty");

        RuleFor(e => e)
            .Must(e => e.Http != null)
            .When(e => string.Equals(e.InvokeMethod, "http", StringComparison.OrdinalIgnoreCase))
            .WithMessage("'http' is null or empty");

        RuleFor(e => e)
            .Must(e => e.Redis != null)
            .When(e => string.Equals(e.InvokeMethod, "redis", StringComparison.OrdinalIgnoreCase))
            .WithMessage("'redis' is null or empty");

        RuleFor(e => e)
            .Must(e => e.RabbitMq != null)
            .When(e => string.Equals(e.InvokeMethod, "rabbitmq", StringComparison.OrdinalIgnoreCase))
            .WithMessage("'rabbitmq' is null or empty");
    }
}

public class PlanarJobRabbitMqPropertiesValidator : AbstractValidator<PlanarJobRabbitMqProperties>
{
    public PlanarJobRabbitMqPropertiesValidator()
    {
        RuleFor(e => e.Exchange)
            .NotEmpty()
            .WithMessage("'exchange' must not be empty");

        RuleFor(e => e.Exchange)
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'exchange' must be 100 characters or fewer. You entered {e.Exchange.Length} characters");

        RuleFor(e => e.RoutingKey)
            .NotEmpty()
            .WithMessage("'routing key' must not be empty");

        RuleFor(e => e.RoutingKey)
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'routing key' must be 100 characters or fewer. You entered {e.RoutingKey.Length} characters");
    }
}

public class PlanarJobRedisPropertiesValidator : AbstractValidator<PlanarJobRedisProperties>
{
    public PlanarJobRedisPropertiesValidator()
    {
        RuleFor(e => e.StreamName)
            .NotEmpty()
            .WithMessage("'stream name' must not be empty");

        RuleFor(e => e.StreamName)
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'stream name' must be 100 characters or fewer. You entered {e.StreamName.Length} characters");

        RuleFor(e => e.ConsumerGroup)
            .NotEmpty()
            .WithMessage("'consumer group' must not be empty");

        RuleFor(e => e.ConsumerGroup)
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'consumer group' must be 100 characters or fewer. You entered {e.StreamName.Length} characters");
    }
}

public class PlanarJobHttpPropertiesValidator : AbstractValidator<PlanarJobHttpProperties>
{
    public PlanarJobHttpPropertiesValidator()
    {
        RuleFor(e => e.BaseUrl)
            .NotEmpty()
            .WithMessage("'base url' must not be empty");

        RuleFor(e => e.BaseUrl)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of 'base url' must be 1000 characters or fewer. You entered {e.BaseUrl.Length} characters");

        RuleFor(e => e.BaseUrl)
            .IsUri()
            .WithMessage("'base url' has invalid url format");

        RuleFor(e => e.Route)
            .NotEmpty()
            .WithMessage("'route' must not be empty");

        RuleFor(e => e.Route)
            .Length(2, 100)
            .WithMessage(e => $"'route' must be between 2 and 100 characters. You entered {e.Route.Length} characters");
    }
}

public class PlanarJobProcessPropertiesValidator : AbstractValidator<PlanarJobProcessProperties>
{
    public PlanarJobProcessPropertiesValidator(ClusterUtil cluster)
    {
        Include(new BaseProcessJobPropertiesValidator(cluster));
    }
}