using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("job")]
    public class JobCliActions : BaseCliAction<JobCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> AddJob(CliAddJobRequest request)
        {
            if (request.Filename == ".") { request.Filename = "JobFile.yml"; }
            var fi = new FileInfo(request.Filename);
            if (fi.Exists == false)
            {
                throw new ApplicationException($"filename '{fi.FullName}' not exists");
            }

            var yml = File.ReadAllText(fi.FullName);
            var prm = new AddJobRequest { Yaml = yml, Path = fi.Directory.FullName };

            var restRequest = new RestRequest("job", Method.Post)
                .AddBody(prm);

            var result = await RestProxy.Invoke<JobId>(restRequest);
            return new CliActionResponse(result, result.Data.Id);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllJobs(CliGetAllJobsRequest request)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest);
            var message = string.Empty;
            CliActionResponse response;
            if (request.Quiet)
            {
                message = string.Join('\n', result.Data?.Select(r => r.Id));
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
        [Action("inspect")]
        public static async Task<CliActionResponse> GetJobDetails(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDetails>(restRequest);
            var tables = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, tables);
        }

        [Action("settings")]
        public static async Task<CliActionResponse> GetJobSettings(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}/settings", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<Dictionary<string, string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("running")]
        public static async Task<CliActionResponse> GetRunningJobs(CliGetRunningJobsRequest request)
        {
            if (request.Iterative && request.Details)
            {
                throw new Exception("running command can't accept both 'iterative' and 'details' parameters");
            }

            var restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest);

            var table =
                request.Details ?
                null :
                CliTableExtensions.GetTable(result.Data);

            var response =
                request.Details ?
                new CliActionResponse(result, serializeObj: result.Data.FirstOrDefault()) :
                new CliActionResponse(result, table);

            return response;
        }

        [Action("stop")]
        public static async Task<CliActionResponse> StopRunningJob(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/stop", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("runninginfo")]
        public static async Task<CliActionResponse> GetRunningInfo(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningInfo/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningInfoResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Information)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Information);
        }

        [Action("runningex")]
        public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningInfo/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningInfoResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Exceptions)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Exceptions);
        }

        [Action("invoke")]
        public static async Task<CliActionResponse> InvokeJob(CliInvokeJobRequest request)
        {
            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddParameter();

            var prm = JsonMapper.Map<InvokeJobRequest, JobOrTriggerKey>(request.GetKey());
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }
            var result = await RestProxy.Invoke<T>(x => x.InvokeJob(prm));
            return new CliActionResponse(result);
        }

        [Action("pauseall")]
        public static async Task<CliActionResponse> PauseAll()
        {
            var restRequest = new RestRequest("job/pauseAll", Method.Post);

            var result = await RestProxy.Invoke<T>(x => x.PauseAll());
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddParameter();

            var prm = jobKey.GetKey();
            var result = await RestProxy.Invoke<T>(x => x.PauseJob(prm));
            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter();

            var prm = jobKey.GetKey();
            var result = await RestProxy.Invoke<T>(x => x.RemoveJob(prm));

            return new CliActionResponse(result);
        }

        [Action("resumeall")]
        public static async Task<CliActionResponse> ResumeAll()
        {
            var restRequest = new RestRequest("job/resumeAll", Method.Post);

            var result = await RestProxy.Invoke<T>(x => x.ResumeAll());
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddParameter();

            var prm = jobKey.GetKey();
            var result = await RestProxy.Invoke<T>(x => x.ResumeJob(prm));
            return new CliActionResponse(result);
        }

        [Action("data")]
        public static async Task<CliActionResponse> UpsertJobData(CliJobDataRequest request)
        {
            var restRequest = new RestRequest("job/xxx", Method.Post);

            BaseResponse result;
            switch (request.Action)
            {
                case JobDataActions.upsert:
                    var prm1 = JsonMapper.Map<JobDataRequest, JobOrTriggerKey>(request.GetKey());
                    prm1.DataValue = request.DataValue;
                    prm1.DataKey = request.DataKey;
                    result = await RestProxy.Invoke<T>(x => x.UpsertJobData(prm1));
                    break;

                case JobDataActions.remove:
                    var prm3 = JsonMapper.Map<RemoveJobDataRequest, JobOrTriggerKey>(request.GetKey());
                    prm3.DataKey = request.DataKey;
                    result = await RestProxy.Invoke<T>(x => x.RemoveJobData(prm3));
                    break;

                case JobDataActions.clear:
                    var prm4 = request.GetKey();
                    result = await RestProxy.Invoke<T>(x => x.ClearJobData(prm4));
                    break;

                default:
                    throw new ApplicationException($"Action {request.Action} is not supported for this command");
            }

            return new CliActionResponse(result);
        }

        [Action("test")]
        public static async Task<CliActionResponse> TestJob(CliInvokeJobRequest request)
        {
            var restRequest = new RestRequest("job/xxx", Method.Post);

            var jobKey = request.GetKey();
            var prm = JsonMapper.Map<InvokeJobRequest, JobOrTriggerKey>(jobKey);
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }
            var invokeDate = DateTime.Now;

            // (1) Invoke job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await RestProxy.Invoke<T>(x => x.InvokeJob(prm));
            if (result.Success == false)
            {
                return new CliActionResponse(result);
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
                instanceId = await RestProxy.Invoke<T>(x => x.GetLastInstanceId(prm1));
                if (instanceId.Success == false)
                {
                    return new CliActionResponse(instanceId);
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
            var runResult = await RestProxy.Invoke<T>(x => x.GetRunningJob(runRequest));
            if (runResult.Success == false) { return new CliActionResponse(runResult); }
            Console.WriteLine();
            var sleepTime = 2000;
            while (runResult.Result != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                AnsiConsole.MarkupLine($" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Result.Progress}[/]%  |  Effected Row(s): [wheat1]{runResult.Result.EffectedRows.GetValueOrDefault()}  |  Run Time: {CliTableFormat.FormatTimeSpan(span)}[/]");
                Thread.Sleep(sleepTime);
                runResult = await RestProxy.Invoke<T>(x => x.GetRunningJob(runRequest));
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
            var status = await RestProxy.Invoke<T>(x => x.GetTestStatus(id));

            if (status.Success == false) { return new CliActionResponse(status); }
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
            table.AddRow($"[grey54]Planar history get[/] [grey62]{instanceId.Result.LogId}[/]");
            table.AddRow($"[grey54]Planar history info[/] [grey62]{instanceId.Result.LogId}[/]");
            table.AddRow($"[grey54]Planar history data[/] [grey62]{instanceId.Result.LogId}[/]");

            if (status.Result.Status == 1)
            {
                table.AddRow($"[grey54]Planar history ex[/] [grey62]{instanceId.Result.LogId}[/]");
            }

            AnsiConsole.Write(table);

            return ActionResponse.Empty;
        }

        [Action("upsertprop")]
        public static async Task<CliActionResponse> UpsertJobProperty(CliUpsertJobPropertyRequest request)
        {
            var restRequest = new RestRequest("job/xxx", Method.Post);

            var prm = JsonMapper.Map<UpsertJobPropertyRequest, CliUpsertJobPropertyRequest>(request);
            return await Execute(x => x.UpsertJobProperty(prm));
        }
    }
}