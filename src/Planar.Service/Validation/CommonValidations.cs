using CommonJob;
using FluentValidation;
using Planar.Common.Exceptions;
using Planar.Service.Exceptions;
using Planar.Service.General;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Validation
{
    public static class CommonValidations
    {
        public static bool EncodingExists<T>(string encoding, ValidationContext<T> context)
        {
            var any = Encoding.GetEncodings().Any(e => e.Name == encoding);
            if (!any)
            {
                context.AddFailure("output encoding", $"encoding '{encoding}' is not valid");
            }

            return any;
        }

        public static async Task<bool> PathExists<T>(string path, ClusterUtil clusterUtil, ValidationContext<T> context)
        {
            try
            {
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

        public static async Task<bool> FilenameExists<T>(IFileJobProperties properties, string filename, ClusterUtil clusterUtil, ValidationContext<T> context)
        {
            try
            {
                ServiceUtil.ValidateJobFileExists(properties.Path, filename);
                await clusterUtil.ValidateJobFileExists(properties.Path, filename);
                return true;
            }
            catch (PlanarException ex)
            {
                context.AddFailure("filename", ex.Message);
                return false;
            }
        }
    }
}