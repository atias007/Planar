using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;

namespace Planar.Service.Model;

public partial class Group
{
    public Roles RoleEnum
    {
        get
        {
            return RoleHelper.GetRoleEnum(Role) ?? Roles.Anonymous;
        }
    }
}