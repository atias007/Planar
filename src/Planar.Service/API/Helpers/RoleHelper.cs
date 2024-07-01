using Planar.API.Common.Entities;
using System;

namespace Planar.Service.API.Helpers;

public static class RoleHelper
{
    public static int? GetRoleValue(string? role)
    {
        if (Enum.TryParse<Roles>(role, ignoreCase: true, out var result))
        {
            return (int)result;
        }

        return null;
    }

    public static string CleanRole(string role)
    {
        if (string.IsNullOrEmpty(role))
        {
            return string.Empty;
        }

        return role.Trim().ToLower();
    }

    public static string DefaultRole => Roles.Anonymous.ToString().ToLower();

    public static Roles? GetRoleEnum(string? role)
    {
        if (Enum.TryParse<Roles>(role, ignoreCase: true, out var result))
        {
            return result;
        }

        return null;
    }
}