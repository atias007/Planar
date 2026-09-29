using FluentValidation;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.Data;
using Planar.Service.General;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Validation;

public static class CommonValidations
{
    public static bool EncodingExists<T>(string encoding, ValidationContext<T> context)
    {
        var any = Array.Exists(Encoding.GetEncodings(), e => e.Name == encoding);
        if (!any)
        {
            context.AddFailure("output encoding", $"encoding '{encoding}' is not valid");
        }

        return any;
    }

    public static bool GlobalConfigExists<T>(string propertyName, string? configKey, ValidationContext<T> context)
    {
if (string.IsNullOrWhiteSpace(configKey)) { return true; }
foreach (var key in Global.GlobalConfig.Keys)
{
    if (string.Equals(key, configKey, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, $"ConnectionStrings:{configKey}", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }
}

context.AddFailure(propertyName, $"global config key '{configKey}', defined at '{propertyName}', does not exist");
return false;
    }

    public static async Task<bool> ResourceExists<T>(string propertyName, string? resourceName, IResourceData resourceData, ValidationContext<T> context)
    {
        if (string.IsNullOrWhiteSpace(resourceName)) { return true; }
        var exists = await resourceData.Exists(resourceName);
        if (exists) { return true; }

        context.AddFailure(propertyName, $"resource '{resourceName}' does not exist");
        return false;
    }

    public static async Task<bool> FilenameExists<T>(string propertyName, string? filename, ClusterUtil clusterUtil, ValidationContext<T> context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                context.AddFailure(propertyName, $"{propertyName} is null or empty");
                return false;
            }

            ServiceUtil.ValidateJobFileExists(filename);
            await clusterUtil.ValidateJobFileExists(filename);
            return true;
        }
        catch (PlanarException ex)
        {
            context.AddFailure(propertyName, ex.Message);
            return false;
        }
    }
}