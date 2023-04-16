using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class AdministratorAuthorizeAttribute : AuthorizeAttribute
    {
        public AdministratorAuthorizeAttribute()
        {
            Policy = Service.Model.DataObjects.Roles.Administrator.ToString();
        }
    }
}