using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Planar.Service.Audit;

public abstract class BaseAuditMessage
{
    protected BaseAuditMessage(IHttpContextAccessor? context)
    {
        Claims = context?.HttpContext?.User?.Claims;
        CliUserName = ExtractRequestHeader(context?.HttpContext, Consts.CliUserName);
        CliUserDomainName = ExtractRequestHeader(context?.HttpContext, Consts.CliUserDomainName);

        // CliIdentity
        if (!string.IsNullOrWhiteSpace(CliUserDomainName) && !string.IsNullOrWhiteSpace(CliUserName)) { CliIdentity = $"{CliUserDomainName}\\{CliUserName}"; }
        else if (string.IsNullOrWhiteSpace(CliUserDomainName)) { CliIdentity = CliUserName; }

        IsAnonymous = string.IsNullOrWhiteSpace(CliIdentity) && (!Claims?.Any(c => c.Type == ClaimTypes.Name) ?? true);
    }

    protected BaseAuditMessage(AuthorizationHandlerContext context)
    {
        Claims = context.User.Claims;
        IsAnonymous = false;
    }

    public IEnumerable<Claim>? Claims { get; private set; }
    public string? CliUserName { get; private set; }
    public string? CliUserDomainName { get; private set; }
    public string? CliIdentity { get; private set; }
    public bool IsAnonymous { get; private set; }

    private static string? ExtractRequestHeader(HttpContext? context, string key)
    {
        try
        {
            if (context == null) { return null; }
            if (!context.Request.Headers.TryGetValue(key, out var result)) { return null; }
            return result.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}