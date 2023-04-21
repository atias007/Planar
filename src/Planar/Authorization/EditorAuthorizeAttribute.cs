using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class EditorAuthorizeAttribute : AuthorizeAttribute
    {
        public EditorAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Editor.ToString();
        }
    }
}