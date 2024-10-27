using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using System;
using System.Linq;
using System.Security.Claims;

namespace Planar.Service.API;

public abstract class BaseLazyBL<TBusinesLayer, TDataLayer>(IServiceProvider serviceProvider) : BaseBL<TBusinesLayer>(serviceProvider)
{
    private readonly Lazy<TDataLayer> _dataLayer = serviceProvider.GetRequiredService<Lazy<TDataLayer>>();
    private readonly IHttpContextAccessor _contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

    protected int? UserId
    {
        get
        {
            return GetClaimIntValue(ClaimTypes.NameIdentifier);
        }
    }

    protected Roles UserRole
    {
        get
        {
            var value = GetClaimIntValue(ClaimTypes.Role) ?? 0;
            if (Enum.IsDefined(typeof(Roles), value))
            {
                return (Roles)value;
            }
            else
            {
                return Roles.Anonymous;
            }
        }
    }

    private int? GetClaimIntValue(string claimType)
    {
        var context = _contextAccessor.HttpContext;
        if (context?.User?.Claims == null) { return null; }
        var claim = context.User.Claims.FirstOrDefault(c => c.Type == claimType);
        if (claim == null) { return null; }
        var strValue = claim.Value;
        if (string.IsNullOrEmpty(strValue)) { return null; }
        var value = RoleHelper.GetRoleValue(strValue);
        return value;
    }

    protected TDataLayer DataLayer => _dataLayer.Value;
}