using Planar.CLI.Attributes;
using System;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("inner", "x")]
    public class InnerCliActions : BaseCliAction<InnerCliActions>
    {
        [Action("cls")]
        public static async Task<CliActionResponse> GetParameter()
        {
            Console.Clear();
            return await Task.FromResult(CliActionResponse.Empty);
        }
    }
}