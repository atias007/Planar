using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class TesterAuthorizeAttribute : AuthorizeAttribute
    {
        public TesterAuthorizeAttribute()
        {
            Policy = API.Common.Entities.Roles.Tester.ToString();
        }
    }
}