using Microsoft.AspNetCore.Authorization;
using Planar.API.Common.Entities;

namespace Planar.Authorization
{
    public class MinimumRoleRequirement : IAuthorizationRequirement
    {
        private readonly Roles _role;

        public MinimumRoleRequirement(Roles role)
        {
            _role = role;
        }

        public Roles Role => _role;
    }
}