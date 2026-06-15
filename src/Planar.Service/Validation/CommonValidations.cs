using CommonJob;
using FluentValidation;
using Planar.Common.Exceptions;
using Planar.Service.General;
using System;
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
}