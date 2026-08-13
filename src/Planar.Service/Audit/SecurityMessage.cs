using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Planar.Service.Services;

namespace Planar.Service.Audit;

public class SecurityMessage : BaseAuditMessage
{
    public SecurityMessage(IHttpContextAccessor context) : base(context)
    {
    }

    public SecurityMessage(AuthorizationHandlerContext context) : base(context)
    {
    }

    public string Title { get; set; } = null!;
    public bool IsWarning { get; set; }
}