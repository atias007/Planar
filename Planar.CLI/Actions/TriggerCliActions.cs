using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
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
        public static async Task<ActionResponse> GetTriggersDetails(CliGetTriggersDetailsRequest request)
        {
            var prm = request.GetKey();
            var result = await Proxy.InvokeAsync(x => x.GetTriggersDetails(prm));
            ActionResponse response;

            var message = string.Empty;
            if (request.Quiet)
            {
                var all = result?.Result?.SimpleTriggers
                    .Select(t => t.Id)
                    .Union(result.Result.CronTriggers.Select(c => c.Id));

                message = string.Join('\n', all);
                response = new ActionResponse(result, message);
            }
            else
            {
                var table = CliTableExtensions.GetTable(result);
                response = new ActionResponse(result, table);
            }

            return response;
        }

        [Action("get")]
        public static async Task<ActionResponse> GetTriggerDetails(CliGetTriggersDetailsRequest request)
        {
            var prm = request.GetKey();
            var result = await Proxy.InvokeAsync(x => x.GetTriggerDetails(prm));

            var trigger1 = result.Result.SimpleTriggers.FirstOrDefault(t => t.Id == request.Id || ($"{t.Group}.{t.Name}" == request.Id));
            if (trigger1 != null) { return new ActionResponse(result, serializeObj: trigger1); }

            var trigger2 = result.Result.CronTriggers.FirstOrDefault(t => t.Id == request.Id || ($"{t.Group}.{t.Name}" == request.Id));
            if (trigger2 != null) { return new ActionResponse(result, serializeObj: trigger2); }

            return new ActionResponse(result);
        }

        [Action("delete")]
        [Action("remove")]
        public static async Task<ActionResponse> RemoveTrigger(CliJobOrTriggerKey request)
        {
            var prm = request.GetKey();
            var result = await Proxy.InvokeAsync(x => x.RemoveTrigger(prm));
            return new ActionResponse(result);
        }

        [Action("pause")]
        public static async Task<ActionResponse> PauseTrigger(CliJobOrTriggerKey request)
        {
            var prm = request.GetKey();
            var result = await Proxy.InvokeAsync(x => x.PauseTrigger(prm));
            return new ActionResponse(result);
        }

        [Action("resume")]
        public static async Task<ActionResponse> ResumeTrigger(CliJobOrTriggerKey request)
        {
            var prm = request.GetKey();
            var result = await Proxy.InvokeAsync(x => x.ResumeTrigger(prm));
            return new ActionResponse(result);
        }

        [Action("upsert")]
        [Action("add")]
        public static async Task<ActionResponse> UpsertTrigger(CliAddTriggerRequest request)
        {
            if (request.Filename == ".") { request.Filename = "JobFile.yml"; }
            var yml = File.ReadAllText(request.Filename);
            var key = request.GetKey();
            var prm = JsonMapper.Map<AddTriggerRequest, JobOrTriggerKey>(key);
            prm.Yaml = yml;
            var result = await Proxy.InvokeAsync(x => x.AddTrigger(prm));
            return new ActionResponse(result);
        }
    }
}