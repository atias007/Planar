using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("trace")]
    public class TraceCliActions : BaseCliAction<TraceCliActions>
    {
        [Action("ls")]
        [Action("list")]
        public static async Task<ActionResponse> GetTrace(CliGetTraceRequest request)
        {
            var prm = JsonMapper.Map<GetTraceRequest, CliGetTraceRequest>(request);
            if (prm.Rows == 0) { prm.Rows = null; };
            if (prm.FromDate == DateTime.MinValue) { prm.FromDate = null; }
            if (prm.ToDate == DateTime.MinValue) { prm.ToDate = null; }

            var result = await Proxy.InvokeAsync(x => x.GetTrace(prm));
            var table = result.GetTable();
            return new ActionResponse(result, table);
        }

        [Action("ex")]
        public static async Task<ActionResponse> GetTraceException(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetTraceException(prm));
            return new ActionResponse(result, serializeObj: result.Result);
        }

        [Action("prop")]
        public static async Task<ActionResponse> GetTraceProperties(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetTraceProperties(prm));
            return new ActionResponse(result, serializeObj: result.Result);
        }
    }
}