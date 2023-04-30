using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class ViewerAuthorizeAttribute : AuthorizeAttribute
    {
        public ViewerAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Viewer.ToString();
        }
    }
}