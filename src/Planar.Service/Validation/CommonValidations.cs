using CommonJob;
using FluentValidation;
using Planar.Common.Exceptions;
using Planar.Service.General;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
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

        public static async Task<bool> PathExists<T>(string? path, ClusterUtil clusterUtil, ValidationContext<T> context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) { return false; }

                ServiceUtil.ValidateJobFolderExists(path);
                await clusterUtil.ValidateJobFolderExists(path);
                return true;
            }
            catch (PlanarException ex)
            {
                context.AddFailure("path", ex.Message);
                return false;
            }
        }

        public static async Task<bool> FilenameExists<T>(IPathJobProperties properties, string propertyName, string? filename, ClusterUtil clusterUtil, ValidationContext<T> context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    context.AddFailure(propertyName, $"{propertyName} is null or empty");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(properties.Path))
                {
                    context.AddFailure("path", "path is null or empty");
                    return false;
                }

                ServiceUtil.ValidateJobFileExists(properties.Path, filename);
                await clusterUtil.ValidateJobFileExists(properties.Path, filename);
                return true;
            }
            catch (PlanarException ex)
            {
                context.AddFailure(propertyName, ex.Message);
                return false;
            }
        }
    }
}