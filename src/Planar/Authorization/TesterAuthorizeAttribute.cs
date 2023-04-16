using Microsoft.AspNetCore.Authorization;

namespace Planar.Authorization
{
    public class TesterAuthorizeAttribute : AuthorizeAttribute
    {
        public TesterAuthorizeAttribute()
        {
            Policy = Service.Model.DataObjects.Roles.Tester.ToString();
        }
    }
}