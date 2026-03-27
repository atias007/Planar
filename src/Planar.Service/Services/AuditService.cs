using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.MapperProfiles;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services;

public class AuditService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly Channel<AuditMessage> _channel = serviceProvider.GetRequiredService<Channel<AuditMessage>>();
    private readonly ILogger<AuditService> _logger = serviceProvider.GetRequiredService<ILogger<AuditService>>();
    private readonly JobKeyHelper _jobKeyHelper = serviceProvider.GetRequiredService<JobKeyHelper>();
    private readonly ISchedulerFactory _schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var reader = _channel.Reader;
            await foreach (var msg in reader.ReadAllAsync(stoppingToken))
            {
                await SafeSaveAudit(msg);
            }
        }
        catch (OperationCanceledException)
        {
            // === DO NOTHING: CLOSE APPLICATION === //
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

    private static string GetTitle(AuditMessage message)
    {
        var surnameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var givenNameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var title = $"{givenNameClaim} {surnameClaim}".Trim();
        if (string.IsNullOrWhiteSpace(title)) { title = message.CliIdentity; }
        if (string.IsNullOrWhiteSpace(title)) { title = RoleHelper.DefaultRole; }
        return title;
    }

    private static string GetUsername(AuditMessage message)
    {
        var usernameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        return usernameClaim ?? message.CliIdentity ?? RoleHelper.DefaultRole;
    }

    private async Task<string?> GetJobId(AuditMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.JobId)) { return message.JobId; }
        if (message.JobKey == null) { return string.Empty; }
        
        var result = await _jobKeyHelper.SafeGetJobId(message.JobKey);
        return result ?? string.Empty;
    }

    private static string GetJobKeyString(JobKey? jobKey)
    {
        if (jobKey == null) { return string.Empty; }
        return $"{jobKey.Group}.{jobKey.Name}";
    }

    private static string? GetAdditionalInfoString(object? additionalInfo)
    {
        if (additionalInfo == null) { return null; }
        return YmlUtil.Serialize(additionalInfo)?.Trim();
    }

    private async Task SaveAudit(AuditMessage message)
    {
        await FillTriggerId(message);

        var jobId = await GetJobId(message);
        var usernameClaim = GetUsername(message);
        var title = GetTitle(message);
        var jobKeyString = GetJobKeyString(message.JobKey);
        var additionalInfoString = GetAdditionalInfoString(message.AdditionalInfo);
        var description = message.Description?.Trim() ?? string.Empty;

        using var scope = serviceScopeFactory.CreateScope();
        var data = scope.ServiceProvider.GetRequiredService<IJobData>();
        var audit = new JobAudit
        {
            DateCreated = DateTime.Now,
            Description = description,
            AdditionalInfo = additionalInfoString,
            JobId = jobId ?? string.Empty,
            Username = usernameClaim,
            UserTitle = title,
            JobKey = jobKeyString
        };

        audit.AdditionalInfo = audit.AdditionalInfo?.Trim();
        if (audit.Description.Length > 200) { audit.Description = audit.Description[0..200]; }
        if (audit.AdditionalInfo?.Length > 4000) { audit.AdditionalInfo = audit.AdditionalInfo[0..4000]; }

        await data.AddJobAudit(audit);
    }

    private async Task FillTriggerId(AuditMessage message)
    {
        if (message.TriggerKey == null) { return; }

        var scheduler = await _schedulerFactory.GetScheduler();
        var trigger = await scheduler.GetTrigger(message.TriggerKey);
        var triggerId = TriggerHelper.GetTriggerId(trigger);
        message.Description = message.Description.Replace("{{TriggerId}}", $"trigger id: {triggerId}");

        if (message.JobKey == null && trigger != null)
        {
            message.JobKey = trigger.JobKey;
        }

        if (message.AddTriggerInfo)
        {
            var info = new List<object>();
            if (message.AdditionalInfo != null) { info.Add(message.AdditionalInfo); }

            var details = GetTriggerDetails(trigger);
            if (details != null) { info.Add(new { trigger = details }); }

            message.AdditionalInfo = info;
        }
    }


    private async Task<TriggerDetails?> GetTriggerDetails(ITrigger? trigger)
    {
        if (trigger == null) { return null; }

        using var scope = serviceScopeFactory.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var scheduler = await _schedulerFactory.GetScheduler();

        if (trigger is ISimpleTrigger t1)
        {
            return await mapper.MapSimpleTriggerDetails(t1, scheduler);
        }

        if (trigger is ICronTrigger t2)
        {
            return await mapper.MapCronTriggerDetails(t2, scheduler);
        }

        return null;
    }
}