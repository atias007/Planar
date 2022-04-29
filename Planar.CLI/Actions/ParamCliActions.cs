using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using System;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("param")]
    public class ParamCliActions : BaseCliAction<ParamCliActions>
    {
        [Action("get")]
        public static async Task<ActionResponse> GetParameter(CliParameterKeyRequest request)
        {
            var prm = JsonMapper.Map<GlobalParameterKey, CliParameterKeyRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetGlobalParameter(prm));
            return new ActionResponse(result, result.Result);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<ActionResponse> GetAllParameter()
        {
            var result = await Proxy.InvokeAsync(x => x.GetAllGlobalParameters());
            return new ActionResponse(result, serializeObj: result.Result);
        }

        [Action("upsert")]
        [Action("add")]
        public static async Task<ActionResponse> UpsertParameter(CliParameterRequest request)
        {
            var prm = JsonMapper.Map<GlobalParameterData, CliParameterRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.UpsertGlobalParameter(prm));
            return new ActionResponse(result);
        }

        [Action("remove")]
        public static async Task<ActionResponse> RemoveParameter(CliParameterKeyRequest request)
        {
            var prm = JsonMapper.Map<GlobalParameterKey, CliParameterKeyRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.RemoveGlobalParameter(prm));
            return new ActionResponse(result);
        }
    }
}