using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services
{
    public class AuditService : BackgroundService
    {
        private readonly Channel<AuditMessage> _channel;
        private readonly ILogger<AuditService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AuditService(Channel<AuditMessage> channel, ILogger<AuditService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _channel = channel;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _channel.Reader;
            while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
            {
                if (reader.TryRead(out var msg))
                {
                    await SafeSaveAudit(msg);
                }
            }

            _channel.Writer.TryComplete();
        }

        private async Task SafeSaveAudit(AuditMessage message)
        {
            try
            {
                await SaveAudit(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to save job audit item. message: {@Message}", message);
            }
        }

        private async Task SaveAudit(AuditMessage message)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<JobData>();
            var audit = new JobAudit
            {
                DateCreated = DateTime.Now,
                Description = message.Description,
                AdditionalInfo = message.AdditionalInfo,
                JobId = message.JobId,
                Username = message.Username,
                UserTitle = message.UserTitle,
            };

            await data.AddJobAudit(audit);
        }
    }
}