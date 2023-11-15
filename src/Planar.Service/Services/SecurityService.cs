using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services
{
    public class SecurityService : BackgroundService
    {
        private readonly Channel<SecurityMessage> _channel;
        private readonly ILogger<SecurityService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SecurityService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
        {
            _channel = serviceProvider.GetRequiredService<Channel<SecurityMessage>>();
            _logger = serviceProvider.GetRequiredService<ILogger<SecurityService>>();
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _channel.Reader;
            while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
            {
                if (reader.TryRead(out var msg))
                {
                    await SafeSaveSecurity(msg);
                }
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

            var usernameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var surnameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var givenNameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var title = $"{givenNameClaim} {surnameClaim}"?.Trim();

            using var scope = _serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<ServiceData>();
            var audit = new SecurityAudit
            {
                DateCreated = DateTime.Now,
                Title = message.Title.Trim(),
                IsWarning = message.IsWarning,
                Username = usernameClaim ?? Roles.Anonymous.ToString().ToLower(),
                UserTitle = title ?? Roles.Anonymous.ToString().ToLower(),
            };

            if (audit.Title.Length > 500) { audit.Title = audit.Title[0..500]; }

            await data.AddSecurityAudit(audit);
        }
    }
}