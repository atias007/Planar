using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Planar.Authorization
{
    public class MinimumRoleHandler(IServiceProvider serviceProvider) : AuthorizationHandler<MinimumRoleRequirement>
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement)
        {
            // No Authorization
            if (AppSettings.Authentication.NoAuthontication)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var claim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (claim == null)
            {
                // View Anonymous Authorization
                if (AppSettings.Authentication.Mode == AuthMode.ViewAnonymous &&
                    Roles.Viewer >= requirement.Role)
                {
                    context.Succeed(requirement);
                }

                // No claim supplied in Authorization mode
                AuditWarningSecuritySafe(context.User.Claims, "no claim(s) supplied with the request while authorization mode activated");
                return Task.CompletedTask;
            }

            // Authorization with supplied claim
            var strValue = claim.Value;
            if (string.IsNullOrEmpty(strValue))
            {
                AuditWarningSecuritySafe(context.User.Claims, "claim(s) with empty value supplied with the request while authorization mode activated");
                return Task.CompletedTask;
            }

            var roleEnum = RoleHelper.GetRoleEnum(strValue);
            if (roleEnum == null)
            {
                AuditWarningSecuritySafe(context.User.Claims, "claim(s) with bad value supplied with the request while authorization mode activated");
                return Task.CompletedTask;
            }

            if (roleEnum == Roles.Anonymous)
            {
                var action = GetCurrentAction();
                AuditWarningSecuritySafe(context.User.Claims, $"user role is not authorize to perform action: {action}. action rule is: {requirement.Role.ToString().ToLower()} while user rule is: anonymous");
                return Task.CompletedTask;
            }

            if (roleEnum >= requirement.Role)
            {
                context.Succeed(requirement);
            }
            else
            {
                var action = GetCurrentAction();
                AuditWarningSecuritySafe(context.User.Claims, $"user role is not authorize to perform action: {action}. action rule is: {requirement.Role.ToString().ToLower()} while user rule is: {roleEnum.ToString().ToLower()}");
            }

            return Task.CompletedTask;
        }

        protected void AuditWarningSecuritySafe(IEnumerable<Claim> claims, string title)
        {
            try
            {
                var audit = new SecurityMessage
                {
                    Claims = claims,
                    Title = title,
                    IsWarning = true
                };

                var producer = _serviceProvider.GetRequiredService<SecurityProducer>();
                producer.Publish(audit);
            }
            catch (Exception ex)
            {
                LogFailureSafe(ex, title);
            }
        }

        private string GetCurrentAction()
        {
            try
            {
                var context = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            }
            catch
            {
                return string.Empty;
            }
        }

        private void LogFailureSafe(Exception ex, string title)
        {
            try
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<MinimumRoleHandler>>();
                logger.LogError(ex, "fail to publish security message. the message: {@Message}", title);
            }
            catch
            {
                // *** DO NOTHING ***
            }
        }
    }
}