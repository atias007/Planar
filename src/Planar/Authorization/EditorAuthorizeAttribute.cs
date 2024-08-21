using Microsoft.AspNetCore.Authorization;
using System;

namespace Planar.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class EditorAuthorizeAttribute : AuthorizeAttribute
    {
        public EditorAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Editor.ToString();
        }
    }
}