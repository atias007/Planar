using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class AdministratorAuthorizeAttribute : AuthorizeAttribute
    {
        public AdministratorAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Administrator.ToString();
        }
    }
}