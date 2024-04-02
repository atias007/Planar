using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using System;
using System.Linq;
using System.Security.Claims;

namespace Planar.Service.API;

public abstract class BaseLazyBL<TBusinesLayer, TDataLayer> : BaseBL<TBusinesLayer>
    where TDataLayer : BaseDataLayer
{
    private readonly Lazy<TDataLayer> _dataLayer;
    private readonly IHttpContextAccessor _contextAccessor;

    protected BaseLazyBL(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _dataLayer = serviceProvider.GetRequiredService<Lazy<TDataLayer>>();
        _contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    }

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
        if (!int.TryParse(strValue, out int value)) { return null; }
        return value;
    }

    protected TDataLayer DataLayer => _dataLayer.Value;
}