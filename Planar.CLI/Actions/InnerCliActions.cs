using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using System;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("inner")]
    public class InnerCliActions : BaseCliAction<InnerCliActions>
    {
        [Action("cls")]
        public static async Task<CliActionResponse> GetParameter(CliParameterKeyRequest request)
        {
            Console.Clear();
            return await Task.FromResult(CliActionResponse.Empty);
        }
    }
}