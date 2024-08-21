using Microsoft.AspNetCore.Authorization;
using System;

namespace Planar.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ViewerAuthorizeAttribute : AuthorizeAttribute
    {
        public ViewerAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Viewer.ToString();
        }
    }
}