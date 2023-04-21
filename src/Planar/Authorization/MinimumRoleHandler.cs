using Microsoft.AspNetCore.Authorization;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model.DataObjects;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Planar.Authorization
{
    public class MinimumRoleHandler : AuthorizationHandler<MinimumRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement)
        {
            // No Authorization
            if (AppSettings.AuthenticationMode == AuthMode.AllAnonymous)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var claim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (claim == null)
            {
                // View Anonymous Authorization
                if (AppSettings.AuthenticationMode == AuthMode.ViewAnonymous &&
                    Roles.Viewer >= requirement.Role)
                {
                    context.Succeed(requirement);
                }

                // No claim supplied in Authorization mode
                return Task.CompletedTask;
            }

            // Authorization with supplied claim
            var strValue = claim.Value;
            if (string.IsNullOrEmpty(strValue)) { return Task.CompletedTask; }
            if (!int.TryParse(strValue, out var roleId)) { return Task.CompletedTask; }
            if (!Enum.IsDefined(typeof(Roles), roleId)) { return Task.CompletedTask; }
            var role = (Roles)roleId;
            if (role == Roles.Anonymous) { return Task.CompletedTask; }

            if (role >= requirement.Role)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}