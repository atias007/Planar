using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("history")]
    public class HistoryCliActions : BaseCliAction<HistoryCliActions>
    {
        [Action("get")]
        public static async Task<ActionResponse> GetHistoryById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetHistoryById(prm));
            return new ActionResponse(result, serializeObj: result.Result);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<ActionResponse> GetHistory(CliGetHistoryRequest request)
        {
            var prm = JsonMapper.Map<GetHistoryRequest, CliGetHistoryRequest>(request);
            if (prm.Rows == 0) { prm.Rows = null; }
            if (prm.FromDate.GetValueOrDefault() == DateTime.MinValue) { prm.FromDate = null; }
            if (prm.ToDate.GetValueOrDefault() == DateTime.MinValue) { prm.ToDate = null; }
            var result = await Proxy.InvokeAsync(x => x.GetHistory(prm));
            var table = result.GetTable();
            return new ActionResponse(result, table);
        }

        [Action("data")]
        public static async Task<ActionResponse> GetHistoryDataById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetHistoryDataById(prm));
            return new ActionResponse(result, serializeObj: result.Result?.Data);
        }

        [Action("info")]
        public static async Task<ActionResponse> GetHistoryInformationById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetHistoryInformationById(prm));
            return new ActionResponse(result, serializeObj: result.Result?.Data);
        }

        [Action("ex")]
        public static async Task<ActionResponse> GetHistoryExceptionById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetHistoryExceptionById(prm));
            return new ActionResponse(result, serializeObj: result.Result?.Data);
        }

        [Action("last")]
        public static async Task<ActionResponse> GetLastHistoryCallForJob(CliGetLastHistoryCallForJobRequest request)
        {
            var prm = JsonMapper.Map<GetLastHistoryCallForJobRequest, CliGetLastHistoryCallForJobRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetLastHistoryCallForJob(prm));
            var table = result.GetTable();
            return new ActionResponse(result, table);
        }
    }
}