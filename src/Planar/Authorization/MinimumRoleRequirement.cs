using Microsoft.AspNetCore.Authorization;
using Planar.API.Common.Entities;

namespace Planar.Authorization
{
    public class MinimumRoleRequirement(Roles role) : IAuthorizationRequirement
    {
        public Roles Role => role;
    }
}