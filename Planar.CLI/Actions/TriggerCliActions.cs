using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("trigger")]
    public class TriggerCliActions : BaseCliAction<TriggerCliActions>
    {
        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetTriggersDetails(CliGetTriggersDetailsRequest request)
        {
            var prm = request.GetKey();
            var restRequest = new RestRequest("trigger/{jobId}/byjob", Method.Get)
                .AddParameter("jobId", prm.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest);
            CliActionResponse response = new(result);

            if (result.IsSuccessful == false)
            {
                return response;
            }

            var message = string.Empty;
            if (request.Quiet)
            {
                var all = result.Data?.SimpleTriggers
                    .Select(t => t.Id)
                    .Union(result.Data.CronTriggers.Select(c => c.Id));

                message = string.Join('\n', all);
                response = new CliActionResponse(result, message);
            }
            else
            {
                var table = CliTableExtensions.GetTable(result.Data);
                response = new CliActionResponse(result, table);
            }

            return response;
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetTriggerDetails(CliGetTriggersDetailsRequest request)
        {
            var prm = request.GetKey();
            var restRequest = new RestRequest("trigger/{triggerId}", Method.Get)
                .AddParameter("triggerId", prm.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest);

            if (result.IsSuccessful == false)
            {
                CliActionResponse response = new(result);
                return response;
            }

            var trigger1 = result.Data.SimpleTriggers.FirstOrDefault(t => t.Id == request.Id || ($"{t.Group}.{t.Name}" == request.Id));
            if (trigger1 != null) { return new CliActionResponse(result, serializeObj: trigger1); }

            var trigger2 = result.Data.CronTriggers.FirstOrDefault(t => t.Id == request.Id || ($"{t.Group}.{t.Name}" == request.Id));
            if (trigger2 != null) { return new CliActionResponse(result, serializeObj: trigger2); }

            return new CliActionResponse(result);
        }

        [Action("delete")]
        [Action("remove")]
        public static async Task<CliActionResponse> RemoveTrigger(CliJobOrTriggerKey request)
        {
            var restRequest = new RestRequest("trigger/{triggerId}", Method.Delete)
                .AddParameter("triggerId", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseTrigger(CliJobOrTriggerKey request)
        {
            var restRequest = new RestRequest("trigger/pause", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeTrigger(CliJobOrTriggerKey request)
        {
            var restRequest = new RestRequest("trigger/resume", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("upsert")]
        [Action("add")]
        public static async Task<CliActionResponse> UpsertTrigger(CliAddTriggerRequest request)
        {
            if (request.Filename == ".") { request.Filename = "JobFile.yml"; }
            var yml = File.ReadAllText(request.Filename);
            var key = request.GetKey();
            var prm = JsonMapper.Map<AddTriggerRequest, JobOrTriggerKey>(key);
            prm.Yaml = yml;

            var restRequest = new RestRequest("trigger", Method.Post).AddBody(prm);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}