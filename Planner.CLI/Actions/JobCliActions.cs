using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("job")]
    public class JobCliActions : BaseCliAction<JobCliActions>
    {
        [Action("add")]
        public static async Task<ActionResponse> AddJob(CliAddJobRequest request)
        {
            if (request.Filename == ".") { request.Filename = "JobFile.yml"; }
            var yml = File.ReadAllText(request.Filename);
            var prm = new AddJobRequest { Yaml = yml };
            var result = await Proxy.InvokeAsync(x => x.AddJob(prm));
            return new ActionResponse(result, result.Result);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<ActionResponse> GetAllJobs(CliGetAllJobsRequest request)
        {
            var result = await Proxy.InvokeAsync(x => x.GetAllJobs());
            var message = string.Empty;
            ActionResponse response;
            if (request.Quiet)
            {
                message = string.Join('\n', result.Result?.Select(r => r.Id));
                response = new ActionResponse(result, message);
            }
            else
            {
                var table = result.GetTable();
                response = new ActionResponse(result, table);
            }

            return response;
        }

        [Action("get")]
        [Action("inspect")]
        public static async Task<ActionResponse> GetJobDetails(CliJobOrTriggerKey jobKey)
        {
            var prm = jobKey.GetKey();
            var result = await Proxy.InvokeAsync(x => x.GetJobDetails(prm));
            var tables = CliTableExtensions.GetTable(result);
            return new ActionResponse(result, tables);
        }

        [Action("settings")]
        public static async Task<ActionResponse> GetJobSettings(CliJobOrTriggerKey jobKey)
        {
            var prm = jobKey.GetKey();
            var result = await Proxy.InvokeAsync(x => x.GetJobSettings(prm));
            return new ActionResponse(result, serializeObj: result.Result);
        }

        [Action("running")]
        public static async Task<ActionResponse> GetRunningJobs(CliGetRunningJobsRequest request)
        {
            if (request.Iterative && request.Details)
            {
                throw new Exception("running command can't accept both 'iterative' and 'details' parameters");
            }

            var prm = JsonMapper.Map<FireInstanceIdRequest, CliGetRunningJobsRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetRunningJobs(prm));

            var table =
                request.Details ?
                null :
                result.GetTable();

            var response =
                request.Details ?
                new ActionResponse(result, serializeObj: result.Result) :
                new ActionResponse(result, table);

            return response;
        }

        [Action("stop")]
        public static async Task<ActionResponse> StopRunningJob(CliFireInstanceIdRequest request)
        {
            var prm = JsonMapper.Map<FireInstanceIdRequest, CliFireInstanceIdRequest>(request);
            return await Execute(x => x.StopRunningJob(prm));
        }

        [Action("runninginfo")]
        public static async Task<ActionResponse> GetRunningInfo(CliFireInstanceIdRequest request)
        {
            var prm = JsonMapper.Map<FireInstanceIdRequest, CliFireInstanceIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetRunningInfo(prm));
            if (string.IsNullOrEmpty(result.Result)) { return new ActionResponse(result); }

            var info = DeserializeObject<RunningInfo>(result.Result);
            return new ActionResponse(result, info.Information);
        }

        [Action("runningex")]
        public static async Task<ActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request)
        {
            var prm = JsonMapper.Map<FireInstanceIdRequest, CliFireInstanceIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetRunningInfo(prm));
            if (string.IsNullOrEmpty(result.Result)) { return new ActionResponse(result); }

            var info = DeserializeObject<RunningInfo>(result.Result);
            var exList = info.Exceptions.Select(e => e.ToString());
            var seperator = string.Empty.PadLeft(80, '=');
            var exInfo = string.Join(seperator, exList.ToArray());
            return new ActionResponse(result, exInfo);
        }

        [Action("invoke")]
        public static async Task<ActionResponse> InvokeJob(CliInvokeJobRequest request)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, JobOrTriggerKey>(request.GetKey());
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }
            var result = await Proxy.InvokeAsync(x => x.InvokeJob(prm));
            return new ActionResponse(result);
        }

        [Action("pauseall")]
        public static async Task<ActionResponse> PauseAll()
        {
            var result = await Proxy.InvokeAsync(x => x.PauseAll());
            return new ActionResponse(result);
        }

        [Action("pause")]
        public static async Task<ActionResponse> PauseJob(CliJobOrTriggerKey jobKey)
        {
            var prm = jobKey.GetKey();
            var result = await Proxy.InvokeAsync(x => x.PauseJob(prm));
            return new ActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<ActionResponse> RemoveJob(CliJobOrTriggerKey jobKey)
        {
            var prm = jobKey.GetKey();
            var result = await Proxy.InvokeAsync(x => x.RemoveJob(prm));
            return new ActionResponse(result);
        }

        [Action("resumeall")]
        public static async Task<ActionResponse> ResumeAll()
        {
            var result = await Proxy.InvokeAsync(x => x.ResumeAll());
            return new ActionResponse(result);
        }

        [Action("resume")]
        public static async Task<ActionResponse> ResumeJob(CliJobOrTriggerKey jobKey)
        {
            var prm = jobKey.GetKey();
            var result = await Proxy.InvokeAsync(x => x.ResumeJob(prm));
            return new ActionResponse(result);
        }

        [Action("data")]
        public static async Task<ActionResponse> UpsertJobData(CliJobDataRequest request)
        {
            BaseResponse result;
            switch (request.Action)
            {
                case JobDataActions.upsert:
                    var prm1 = JsonMapper.Map<JobDataRequest, JobOrTriggerKey>(request.GetKey());
                    prm1.DataValue = request.DataValue;
                    prm1.DataKey = request.DataKey;
                    result = await Proxy.InvokeAsync(x => x.UpsertJobData(prm1));
                    break;

                case JobDataActions.remove:
                    var prm3 = JsonMapper.Map<RemoveJobDataRequest, JobOrTriggerKey>(request.GetKey());
                    prm3.DataKey = request.DataKey;
                    result = await Proxy.InvokeAsync(x => x.RemoveJobData(prm3));
                    break;

                case JobDataActions.clear:
                    var prm4 = request.GetKey();
                    result = await Proxy.InvokeAsync(x => x.ClearJobData(prm4));
                    break;

                default:
                    throw new ApplicationException($"Action {request.Action} is not supported for this command");
            }

            return new ActionResponse(result);
        }

        [Action("test")]
        public static async Task<ActionResponse> TestJob(CliInvokeJobRequest request)
        {
            var jobKey = request.GetKey();
            var prm = JsonMapper.Map<InvokeJobRequest, JobOrTriggerKey>(jobKey);
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }
            var invokeDate = DateTime.Now;

            // (1) Invoke job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await Proxy.InvokeAsync(x => x.InvokeJob(prm));
            if (result.Success == false)
            {
                return new ActionResponse(result);
            }

            // (2) Sleep 1 sec
            await Task.Delay(1000);

            // (3) Get instance id
            var prm1 = JsonMapper.Map<GetLastInstanceIdRequest, JobOrTriggerKey>(jobKey);
            prm1.InvokeDate = invokeDate;
            AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id... ");
            BaseResponse<LastInstanceId> instanceId = null;
            for (int i = 0; i < 3; i++)
            {
                instanceId = await Proxy.InvokeAsync(x => x.GetLastInstanceId(prm1));
                if (instanceId.Success == false)
                {
                    return new ActionResponse(instanceId);
                }

                if (instanceId.Result != null) break;
            }

            if (instanceId.Result == null)
            {
                AnsiConsole.WriteLine();
                throw new ApplicationException("Could not found running instance id");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{instanceId.Result.InstanceId}[/]");

            // (4) Get running info
            var runRequest = new FireInstanceIdRequest { FireInstanceId = instanceId.Result.InstanceId };
            var runResult = await Proxy.InvokeAsync(x => x.GetRunningJob(runRequest));
            if (runResult.Success == false) { return new ActionResponse(runResult); }
            Console.WriteLine();
            var sleepTime = 2000;
            while (runResult.Success && runResult.Result != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                AnsiConsole.MarkupLine($" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Result.Progress}[/]%  |  Effected Row(s): [wheat1]{runResult.Result.EffectedRows.GetValueOrDefault()}  |  Run Time: {CliTableFormat.FormatTimeSpan(span)}[/]");
                Thread.Sleep(sleepTime);
                runResult = await Proxy.InvokeAsync(x => x.GetRunningJob(runRequest));
                if (runResult.Success == false) { break; }
                if (span.TotalMinutes >= 5) { sleepTime = 10000; }
                else if (span.TotalMinutes >= 15) { sleepTime = 20000; }
                else if (span.TotalMinutes >= 30) { sleepTime = 30000; }
            }

            Console.CursorTop -= 1;
            AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: [green]100%[/]  |  ");

            // (5) Sleep 1 sec
            await Task.Delay(1000);

            // (6) Check log
            var id = new GetByIdRequest { Id = instanceId.Result.LogId };
            var status = await Proxy.InvokeAsync(x => x.GetTestStatus(id));

            if (status.Success == false) { return new ActionResponse(status); }
            if (status.Result == null)
            {
                Console.WriteLine();
                throw new ApplicationException($"Could not found log data for log id {id.Id}");
            }

            var finalSpan = TimeSpan.FromMilliseconds(status.Result.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {status.Result.EffectedRows.GetValueOrDefault()}");
            AnsiConsole.MarkupLine($"  |  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}");
            AnsiConsole.Markup(" [gold3_1][[x]][/] ");
            if (status.Result.Status == 0)
            {
                AnsiConsole.Markup("[green]Success[/]");
            }
            else
            {
                AnsiConsole.Markup($"[red]Fail (status {status.Result.Status})[/]");
            }

            Console.WriteLine();
            Console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
            table.BorderColor(Color.FromInt32(242));
            table.AddRow($"[grey54]planner history get[/] [grey62]{instanceId.Result.LogId}[/]");
            table.AddRow($"[grey54]planner history info[/] [grey62]{instanceId.Result.LogId}[/]");
            table.AddRow($"[grey54]planner history data[/] [grey62]{instanceId.Result.LogId}[/]");

            if (status.Result.Status == 1)
            {
                table.AddRow($"[grey54]planner history ex[/] [grey62]{instanceId.Result.LogId}[/]");
            }

            AnsiConsole.Write(table);

            return ActionResponse.Empty;
        }

        [Action("upsertprop")]
        public static async Task<ActionResponse> UpsertJobProperty(CliUpsertJobPropertyRequest request)
        {
            var prm = JsonMapper.Map<UpsertJobPropertyRequest, CliUpsertJobPropertyRequest>(request);
            return await Execute(x => x.UpsertJobProperty(prm));
        }
    }
}