using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class ViewerAuthorizeAttribute : AuthorizeAttribute
    {
        public ViewerAuthorizeAttribute()
        {
            Policy = Service.Model.DataObjects.Roles.Viewer.ToString();
        }
    }
}