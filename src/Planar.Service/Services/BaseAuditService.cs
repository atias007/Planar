using Microsoft.Extensions.Hosting;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using System.Linq;
using System.Security.Claims;

namespace Planar.Service.Services;

public abstract class BaseAuditService : BackgroundService
{
    protected static string GetTitle(BaseAuditMessage message)
    {
        var surnameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var givenNameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var title = $"{givenNameClaim} {surnameClaim}".Trim();
        if (title.Length > 500) { title = title[0..500]; }
        if (!string.IsNullOrWhiteSpace(title)) { return title; }
        if (!string.IsNullOrWhiteSpace(message.CliIdentity)) { return message.CliIdentity; }
        return RoleHelper.DefaultRole;
    }

    protected static string GetUsername(BaseAuditMessage message)
    {
        var usernameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(usernameClaim)) { return usernameClaim; }
        if (!string.IsNullOrWhiteSpace(message.CliIdentity)) { return message.CliIdentity; }
        return RoleHelper.DefaultRole;
    }

    protected static string? GetAdditionalInfoString(object? additionalInfo)
    {
        if (additionalInfo == null) { return null; }
        return YmlUtil.Serialize(additionalInfo)?.Trim();
    }
}