using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services;

public class SecurityService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : BaseAuditService
{
    private readonly Channel<SecurityMessage> _channel = serviceProvider.GetRequiredService<Channel<SecurityMessage>>();
    private readonly ILogger<SecurityService> _logger = serviceProvider.GetRequiredService<ILogger<SecurityService>>();
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var reader = _channel.Reader;
            await foreach (var msg in reader.ReadAllAsync(stoppingToken))
            {
                await SafeSaveSecurity(msg);
            }
        }
        catch (OperationCanceledException)
        {
            // === DO NOTHING: CLOSE APPLICATION === //
        }

        _channel.Writer.TryComplete();
    }

    private async Task SafeSaveSecurity(SecurityMessage message)
    {
        try
        {
            await SaveAudit(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to save security audit item. message: {@Message}", message);
        }
    }

    private async Task SaveAudit(SecurityMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Title))
        {
            _logger.LogWarning("security audit item has no title. message: {@Message}", message);
            return;
        }

        var usernameClaim = GetUsername(message);
        var title = GetTitle(message);

        using var scope = _serviceScopeFactory.CreateScope();
        var data = scope.ServiceProvider.GetRequiredService<IServiceData>();
        var audit = new SecurityAudit
        {
            DateCreated = DateTime.Now,
            Title = message.Title.Trim(),
            IsWarning = message.IsWarning,
            Username = usernameClaim,
            UserTitle = title,
        };

        await data.AddSecurityAudit(audit);
    }
}