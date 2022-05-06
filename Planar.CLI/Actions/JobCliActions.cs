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
            var result = await GetRunningJobsInner(request);

            var table =
                request.Details ?
                null :
                CliTableExtensions.GetTable(result.Item1);

            var response =
               request.Details ?
               new CliActionResponse(result.Item2, serializeObj: result.Item1) :
               new CliActionResponse(result.Item2, table);

            return response;
        }

        internal static async Task<(List<RunningJobDetails>, RestResponse)> GetRunningJobsInner(CliGetRunningJobsRequest request)
        {
            if (request.Iterative && request.Details)
            {
                throw new Exception("running command can't accept both 'iterative' and 'details' parameters");
            }

            RestRequest restRequest;
            RestResponse restResponse;
            List<RunningJobDetails> resultData;

            if (string.IsNullOrEmpty(request.FireInstanceId))
            {
                restRequest = new RestRequest("job/running", Method.Get);
                var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest);
                resultData = result.Data;
                restResponse = result;
            }
            else
            {
                restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                    .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);
                var result = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                resultData = new List<RunningJobDetails> { result.Data };
                restResponse = result;
            }

            return (resultData, restResponse);
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
            var result = await InvokeJobInner(request);
            return new CliActionResponse(result);
        }

        [Action("pauseall")]
        public static async Task<CliActionResponse> PauseAll()
        {
            var restRequest = new RestRequest("job/pauseAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resumeall")]
        public static async Task<CliActionResponse> ResumeAll()
        {
            var restRequest = new RestRequest("job/resumeAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("data")]
        public static async Task<CliActionResponse> UpsertJobData(CliJobDataRequest request)
        {
            RestResponse result;
            switch (request.Action)
            {
                case JobDataActions.upsert:
                    var prm1 = new JobDataRequest
                    {
                        Id = request.Id,
                        DataKey = request.DataKey,
                        DataValue = request.DataValue
                    };

                    var restRequest1 = new RestRequest("job/data", Method.Post).AddBody(prm1);
                    result = await RestProxy.Invoke(restRequest1);
                    break;

                case JobDataActions.remove:
                    var restRequest2 = new RestRequest("job/{id}/data/{key}", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment)
                        .AddParameter("key", request.DataKey, ParameterType.UrlSegment);

                    result = await RestProxy.Invoke(restRequest2);
                    break;

                case JobDataActions.clear:
                    var restRequest3 = new RestRequest("job/{id}/allData", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment);
                    result = await RestProxy.Invoke(restRequest3);
                    break;

                default:
                    throw new ApplicationException($"Action {request.Action} is not supported for this command");
            }

            return new CliActionResponse(result);
        }

        private static async Task<RestResponse> InvokeJobInner(CliInvokeJobRequest request)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, CliInvokeJobRequest>(request);
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest);
            return result;
        }

        private static async Task<RestResponse<LastInstanceId>> GetLastInstanceId(string id, DateTime invokeDate)
        {
            var restRequest = new RestRequest("job/{id}/lastInstanceId", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", invokeDate, ParameterType.QueryString);
            var result = await RestProxy.Invoke<LastInstanceId>(restRequest);
            return result;
        }

        [Action("test")]
        public static async Task<CliActionResponse> TestJob(CliInvokeJobRequest request)
        {
            // (1) Invoke job
            var invokeDate = DateTime.Now;
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await InvokeJobInner(request);
            if (result.IsSuccessful == false)
            {
                return new CliActionResponse(result);
            }

            // (2) Sleep 1 sec
            await Task.Delay(1000);

            // (3) Get instance id
            AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id... ");
            RestResponse<LastInstanceId> instanceId = null;
            for (int i = 0; i < 3; i++)
            {
                instanceId = await GetLastInstanceId(request.Id, invokeDate);
                if (instanceId.IsSuccessful == false)
                {
                    return new CliActionResponse(instanceId);
                }

                if (instanceId.Data != null) break;
            }

            if (instanceId.Data == null)
            {
                AnsiConsole.WriteLine();
                throw new ApplicationException("Could not found running instance id");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{instanceId.Data.InstanceId}[/]");

            // (4) Get running info
            var restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                .AddParameter("instanceId", instanceId.Data.InstanceId, ParameterType.UrlSegment);
            var runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);

            if (runResult.IsSuccessful == false) { return new CliActionResponse(runResult); }
            Console.WriteLine();
            var sleepTime = 2000;
            while (runResult.Data != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                AnsiConsole.MarkupLine($" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Data.Progress}[/]%  |  Effected Row(s): [wheat1]{runResult.Data.EffectedRows.GetValueOrDefault()}  |  Run Time: {CliTableFormat.FormatTimeSpan(span)}[/]");
                Thread.Sleep(sleepTime);
                runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                if (runResult.IsSuccessful == false) { break; }
                if (span.TotalMinutes >= 5) { sleepTime = 10000; }
                else if (span.TotalMinutes >= 15) { sleepTime = 20000; }
                else if (span.TotalMinutes >= 30) { sleepTime = 30000; }
            }

            Console.CursorTop -= 1;
            AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: [green]100%[/]  |  ");

            // (5) Sleep 1 sec
            await Task.Delay(1000);

            // (6) Check log
            var restTestRequest = new RestRequest("job/testStatus/{id}", Method.Get)
                .AddParameter("id", instanceId.Data.LogId, ParameterType.UrlSegment);
            var status = await RestProxy.Invoke<GetTestStatusResponse>(restTestRequest);

            if (status.IsSuccessful == false) { return new CliActionResponse(status); }
            if (status.Data == null)
            {
                Console.WriteLine();
                throw new ApplicationException($"Could not found log data for log id {instanceId.Data.LogId}");
            }

            var finalSpan = TimeSpan.FromMilliseconds(status.Data.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {status.Data.EffectedRows.GetValueOrDefault()}");
            AnsiConsole.MarkupLine($"  |  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}");
            AnsiConsole.Markup(" [gold3_1][[x]][/] ");
            if (status.Data.Status == 0)
            {
                AnsiConsole.Markup("[green]Success[/]");
            }
            else
            {
                AnsiConsole.Markup($"[red]Fail (status {status.Data.Status})[/]");
            }

            Console.WriteLine();
            Console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
            table.BorderColor(Color.FromInt32(242));
            table.AddRow($"[grey54]Planar history get[/] [grey62]{instanceId.Data.LogId}[/]");
            table.AddRow($"[grey54]Planar history info[/] [grey62]{instanceId.Data.LogId}[/]");
            table.AddRow($"[grey54]Planar history data[/] [grey62]{instanceId.Data.LogId}[/]");

            if (status.Data.Status == 1)
            {
                table.AddRow($"[grey54]Planar history ex[/] [grey62]{instanceId.Data.LogId}[/]");
            }

            AnsiConsole.Write(table);

            return CliActionResponse.Empty;
        }

        [Action("upsertprop")]
        public static async Task<CliActionResponse> UpsertJobProperty(CliUpsertJobPropertyRequest request)
        {
            var restRequest = new RestRequest("job/property", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}