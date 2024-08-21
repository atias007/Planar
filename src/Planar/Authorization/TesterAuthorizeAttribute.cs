using Microsoft.AspNetCore.Authorization;
using System;

namespace Planar.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TesterAuthorizeAttribute : AuthorizeAttribute
    {
        public TesterAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Tester.ToString();
        }
    }
}