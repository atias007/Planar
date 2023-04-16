using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class EditorAuthorizeAttribute : AuthorizeAttribute
    {
        public EditorAuthorizeAttribute()
        {
            Policy = Service.Model.DataObjects.Roles.Editor.ToString();
        }
    }
}