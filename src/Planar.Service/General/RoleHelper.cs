using Planar.API.Common.Entities;

namespace Planar.Service.General
{
    internal static class RoleHelper
    {
        public static string GetTitle(int roleId)
        {
            return ((Roles)roleId).ToString().ToLower();
        }
    }
}