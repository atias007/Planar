using Microsoft.AspNetCore.Authorization;
using System;

namespace Planar.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AdministratorAuthorizeAttribute : AuthorizeAttribute
    {
        public AdministratorAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Administrator.ToString();
        }
    }
}