using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Model;

public partial class Group
{
    public string Role
    {
        get
        {
            try
            {
                var en = (Roles)Enum.ToObject(typeof(Roles), RoleId);
                return en.ToString().ToLower();
            }
            catch
            {
                return Roles.Anonymous.ToString().ToLower();
            }
        }
    }
}