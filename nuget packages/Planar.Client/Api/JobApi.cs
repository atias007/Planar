using Planar.Client.Entities;
using Planar.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class JobApi : BaseApi, IJobApi
    {
        public JobApi(RestProxy proxy) : base(proxy)
        {
        }

#if NETSTANDARD2_0

        public async Task<string> AddAsync(string folder, string jobFileName, CancellationToken cancellationToken = default)
#else
        public async Task<string> AddAsync(string folder, string? jobFileName, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(folder, nameof(folder));
            var body = new
            {
                folder,
                jobFileName
            };

            var restRequest = new RestRequest("job/folder", HttpMethod.Post)
                .AddBody(body);

            var result = await _proxy.InvokeAsync<PlanarStringIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

        public async Task CancelRunningAsync(string fireInstanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(fireInstanceId, nameof(fireInstanceId));
            var restRequest = new RestRequest("job/cancel", HttpMethod.Post)
                .AddBody(new { fireInstanceId });
            await _proxy.InvokeAsync<JobDescription>(restRequest, cancellationToken);
        }

        public async Task ClearDataAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/data", HttpMethod.Delete)
                       .AddSegmentParameter("id", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}", HttpMethod.Delete)
                .AddSegmentParameter("id", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(key, nameof(key));
            var restRequest = new RestRequest("job/{id}/data/{key}", HttpMethod.Delete)
                       .AddSegmentParameter("id", id)
                       .AddSegmentParameter("key", key);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<JobDescription> DescribeJobAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/info", HttpMethod.Get)
               .AddSegmentParameter("id", id);
            var result = await _proxy.InvokeAsync<JobDescription>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<KeyValueItem>> GatSettingsAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/settings", HttpMethod.Get)
               .AddSegmentParameter("id", id);

            var result = await _proxy.InvokeAsync<IEnumerable<KeyValueItem>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<JobDetails> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}", HttpMethod.Get)
                .AddSegmentParameter("id", id);

            var result = await _proxy.InvokeAsync<JobDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<JobAuditWithInformation> GetAuditAsync(int auditId, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/audit/{auditId}", HttpMethod.Get)
                .AddSegmentParameter("auditId", auditId);
            var result = await _proxy.InvokeAsync<JobAuditWithInformation>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetJobFileAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            var restRequest = new RestRequest("job/jobfile/{name}", HttpMethod.Get)
                .AddSegmentParameter("name", name);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<string>> GetJobTypesAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/types", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<string>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<DateTime?> GetNextRunningAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/next-running", HttpMethod.Get)
                .AddSegmentParameter("id", id);

            var result = await _proxy.InvokeScalarAsync<DateTime>(restRequest, cancellationToken);
            return result;
        }

        public async Task<DateTime?> GetPreviousRunningAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/prev-running", HttpMethod.Get)
               .AddSegmentParameter("id", id);

            var result = await _proxy.InvokeScalarAsync<DateTime>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<RunningJobDetails>> GetRunningAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/running", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<RunningJobDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<RunningJobDetails> GetRunningAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("job/running-instance/{instanceId}", HttpMethod.Get)
                    .AddSegmentParameter("instanceId", instanceId);
            var result = await _proxy.InvokeAsync<RunningJobDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<RunningJobData> GetRunningDataAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("job/running-data/{instanceId}", HttpMethod.Get)
                .AddSegmentParameter("instanceId", instanceId);

            var result = await _proxy.InvokeAsync<RunningJobData>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task InvokeAsync(string id, DateTime? nowOverrideValue = null, Dictionary<string, string> data = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
#else
        public async Task InvokeAsync(string id, DateTime? nowOverrideValue = null, Dictionary<string, string?>? data = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
#endif

        {
            ValidateMandatory(id, nameof(id));
            var request = new
            {
                id,
                nowOverrideValue,
                timeout,
                data
            };

            var restRequest = new RestRequest("job/invoke", HttpMethod.Post)
                .AddBody(request);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<IPagingResponse<JobAudit>> ListAllAuditsAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default)
        {
            var paging = new Paging(pageNumber, pageSize);
            paging.SetPagingDefaults();
            var restRequest = new RestRequest("job/audits", HttpMethod.Get)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<JobAudit>>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task<IPagingResponse<JobBasicDetails>> ListAsync(ListJobsFilter filter = null, CancellationToken cancellationToken = default)
#else
        public async Task<IPagingResponse<JobBasicDetails>> ListAsync(ListJobsFilter? filter = null, CancellationToken cancellationToken = default)
#endif
        {
            var f = filter ?? ListJobsFilter.Empty;
            var restRequest = new RestRequest("job", HttpMethod.Get);
            restRequest.AddQueryParameter("jobCategory", (int)f.Category);
            if (!string.IsNullOrEmpty(f.JobType))
            {
                restRequest.AddQueryParameter("jobType", f.JobType);
            }

            if (!string.IsNullOrEmpty(f.Group))
            {
                restRequest.AddQueryParameter("group", f.Group);
            }

            if (f.Active.GetValueOrDefault() ^ f.Inactive.GetValueOrDefault()) // XOR Operator
            {
                if (f.Active.GetValueOrDefault())
                {
                    restRequest.AddQueryParameter("active", true.ToString());
                }

                if (f.Inactive.GetValueOrDefault())
                {
                    restRequest.AddQueryParameter("active", false.ToString());
                }
            }

            if (!string.IsNullOrWhiteSpace(f.Filter))
            {
                restRequest.AddQueryParameter("filter", f.Filter);
            }

            restRequest.AddQueryPagingParameter(f);

            var result = await _proxy.InvokeAsync<PagingResponse<JobBasicDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IPagingResponse<JobAudit>> ListAuditsAsync(
            string id,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var paging = new Paging(pageNumber, pageSize);
            var restRequest = new RestRequest("job/{id}/audit", HttpMethod.Get)
                .AddSegmentParameter("id", id)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<JobAudit>>(restRequest, cancellationToken);
            return result;
        }

        public async Task PauseAsync(string id, DateTime? autoResumeDate = null, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/pause", HttpMethod.Post)
                .AddBody(new { id, autoResumeDate });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task PauseGroupAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            var restRequest = new RestRequest("job/pause-group", HttpMethod.Post)
               .AddBody(new { name });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task PutDataAsync(string id, string key, string value, CancellationToken cancellationToken = default)
#else
		public async Task PutDataAsync(string id, string key, string? value, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(key, nameof(key));
            var prm = new
            {
                Id = id,
                DataKey = key,
                DataValue = value
            };

            var restRequest = new RestRequest("job/data", HttpMethod.Post).AddBody(prm);
            try
            {
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
            catch (PlanarConflictException)
            {
                restRequest = new RestRequest("job/data", HttpMethod.Put).AddBody(prm);
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
        }

#if NETSTANDARD2_0

        public async Task<string> QueueInvokeAsync(
            string id,
            DateTime dueDate,
            TimeSpan? timeout,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string> data = null,
            CancellationToken cancellationToken = default)
#else
        public async Task<string> QueueInvokeAsync(
            string id,
            DateTime dueDate,
            TimeSpan? timeout,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string?>? data = null,
            CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(id, nameof(id));
            var request = new
            {
                id,
                dueDate,
                timeout,
                nowOverrideValue,
                data
            };

            var restRequest = new RestRequest("job/queue-invoke", HttpMethod.Post)
                .AddBody(request);
            var result = await _proxy.InvokeAsync<PlanarStringIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

#if NETSTANDARD2_0

        public async Task WaitAsync(string id, string group, CancellationToken cancellationToken = default)

#else
        public async Task WaitAsync(string? id, string? group, CancellationToken cancellationToken = default)

#endif

        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(group, nameof(group));

            var restRequest = new RestRequest("job/wait", HttpMethod.Get);
            if (!string.IsNullOrWhiteSpace(id)) { restRequest.AddQueryParameter("id", id); }
            if (!string.IsNullOrWhiteSpace(group)) { restRequest.AddQueryParameter("group", group); }
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ResumeAsync(string id, DateTime? autoResumeDate = null, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/resume", HttpMethod.Post)
               .AddBody(new { id, autoResumeDate });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ResumeGroupAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            var restRequest = new RestRequest("job/resume-group", HttpMethod.Post)
               .AddBody(new { name });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task SetAutoResumeAsync(string id, DateTime autoResumeDate, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(autoResumeDate, nameof(autoResumeDate));

            var restRequest = new RestRequest("job/auto-resume", HttpMethod.Post)
                .AddBody(new { id, autoResumeDate });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task CancelAutoResumeAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/auto-resume", HttpMethod.Delete)
                .AddSegmentParameter("id", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task SetAuthor(string id, string author, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(author, nameof(author));
            var restRequest = new RestRequest("job/author", HttpPatchMethod)
                .AddBody(new
                {
                    id,
                    author
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task TestAsync(string id, Func<RunningJobDetails, Task> callback, DateTime? nowOverrideValue = null, Dictionary<string, string> data = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
#else
        public async Task TestAsync(string id, Func<RunningJobDetails, Task> callback, DateTime? nowOverrideValue = null, Dictionary<string, string?>? data = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(id, nameof(id));
            var invokeDate = DateTime.Now.AddSeconds(-1);

            // (0) Check the job
            await CheckAlreadyRunningJobInner(id, cancellationToken);

            // (1) Invoke job
            await InvokeJobInner(id, nowOverrideValue, data, timeout, cancellationToken);

            // (2) Get last instance id
            var lastInstanceId = await GetLastInstanceId(id, invokeDate, cancellationToken);
            var instanceId = lastInstanceId.InstanceId;
            var logId = lastInstanceId.LogId;

            // (3) Get running info
            await GetRunningData(callback, instanceId, invokeDate, logId, cancellationToken);

            // (4) Sleep 1 sec
            await Task.Delay(500, cancellationToken);

            // (5) Check log
            await CheckLog(callback, logId, cancellationToken);
        }

        public async Task<string> UpdateAsync(string id, bool updateJobData = false, bool updateTriggersData = false, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var body = new
            {
                id,
                options = new
                {
                    updateJobData,
                    updateTriggersData
                }
            };

            var restRequest = new RestRequest("job", HttpMethod.Put)
                .AddBody(body);

            var result = await _proxy.InvokeAsync<PlanarStringIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

#if NETSTANDARD2_0

        private static DateTime? GetEstimatedEndTime(RunningJobDetails data)
#else
        private static DateTime? GetEstimatedEndTime(RunningJobDetails? data)
#endif
        {
            if (data == null) { return null; }
            if (data.EstimatedEndTime == null) { return null; }
            var estimateEnd = DateTime.Now.Add(data.EstimatedEndTime.Value);
            return estimateEnd;
        }

        private async Task CheckAlreadyRunningJobInner(string id, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<RunningJobDetails>>(restRequest, cancellationToken);

            var exists = result.Exists(d => d.Id == id || string.Equals($"{d.Group}.{d.Name}", id, StringComparison.OrdinalIgnoreCase));
            if (exists) { throw new PlanarException($"job id {id} already running. test can not be invoked until job done"); }
        }

        private async Task CheckLog(Func<RunningJobDetails, Task> callback, long logId, CancellationToken cancellationToken)
        {
            var restTestRequest = new RestRequest("history/{id}", HttpMethod.Get)
               .AddSegmentParameter("id", logId);
            HistoryDetails status;

            try
            {
                status = await _proxy.InvokeAsync<HistoryDetails>(restTestRequest, cancellationToken);
            }
            catch (PlanarNotFoundException)
            {
                throw new PlanarNotFoundException($"could not found log data for log id {logId}");
            }

            var details = new RunningJobDetails
            {
                EffectedRows = status.EffectedRows,
                ExceptionsCount = status.ExceptionCount,
                FireInstanceId = status.InstanceId,
                FireTime = status.StartDate,
                Group = status.JobGroup,
                Id = status.JobId,
                JobType = status.JobType,
                Name = status.JobName,
                Progress = 100,
                RunTime = TimeSpan.FromMilliseconds(status.Duration.GetValueOrDefault()),
                TriggerGroup = status.TriggerGroup,
                TriggerId = status.TriggerId,
                TriggerName = status.TriggerName
            };

            _ = callback(details);
        }

        private async Task<LastInstanceId> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/last-instance-id/long-polling", HttpMethod.Get)
                .AddSegmentParameter("id", id)
                .AddQueryParameter("invokeDate", dateParameter)
                .SetTimeoutSeconds(40);

            try
            {
                var result = await _proxy.InvokeAsync<LastInstanceId>(restRequest, cancellationToken)
                    ?? throw new PlanarException("could not found running instance id. check whether job is paused or maybe another instance already running");

                return result;
            }
            catch (PlanarConflictException)
            {
                throw new PlanarConflictException($"job id {id} already running. test can not be invoked until job done");
            }
        }

        private async Task GetRunningData(Func<RunningJobDetails, Task> callback, string instanceId, DateTime invokeDate, long logId, CancellationToken cancellationToken)
        {
            // check for very fast finish job
            var result = await InitGetRunningData(instanceId, cancellationToken);
            if (result.Item1 || result.Item2 == null)
            {
                await CheckLog(callback, logId, cancellationToken);
                return;
            }

            Task dataTask = Task.CompletedTask;
            var runResult = result.Item2;
            DateTime? estimateEnd = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (dataTask.Status == TaskStatus.RanToCompletion)
                {
                    dataTask = Task.Run(async () =>
                    {
                        var data = await LongPollingGetRunningData(callback, runResult, instanceId, invokeDate, cancellationToken);
                        runResult = data.Item1;
                        estimateEnd = data.Item2;
                    }, cancellationToken);
                }

                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(200, cancellationToken);
                    if (dataTask.Status == TaskStatus.RanToCompletion) { break; }
                }

                if (runResult == null)
                {
                    var isRunning = await IsHistoryStatusRunning(logId, cancellationToken);
                    if (!isRunning) { break; }
                }
            }
        }

#if NETSTANDARD2_0

        private async Task<(bool, RunningJobDetails)> InitGetRunningData(string instanceId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running-instance/{instanceId}", HttpMethod.Get)
                .AddSegmentParameter("instanceId", instanceId);

            RunningJobDetails runResult;
#else
        private async Task<(bool, RunningJobDetails?)> InitGetRunningData(string instanceId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running-instance/{instanceId}", HttpMethod.Get)
                .AddSegmentParameter("instanceId", instanceId);

            RunningJobDetails? runResult;
#endif

            try
            {
                try
                {
                    runResult = await _proxy.InvokeAsync<RunningJobDetails>(restRequest, cancellationToken);
                }
                catch (PlanarNotFoundException)
                {
                    await Task.Delay(1000, cancellationToken);
                    runResult = await _proxy.InvokeAsync<RunningJobDetails>(restRequest, cancellationToken);
                }
            }
            catch (PlanarNotFoundException)
            {
                return (true, null);
            }

            return (false, runResult);
        }

#if NETSTANDARD2_0

        private static void InvokeCallback(Func<RunningJobDetails, Task> callback, RunningJobDetails data, DateTime invokeDate, DateTime? estimateEnd)
#else
        private static void InvokeCallback(Func<RunningJobDetails, Task> callback, RunningJobDetails? data, DateTime invokeDate, DateTime? estimateEnd)
#endif
        {
            if (data == null) { return; }

            var span = DateTimeOffset.Now.Subtract(invokeDate);
            var endSpan = estimateEnd == null ? data.EstimatedEndTime : estimateEnd.Value.Subtract(DateTime.Now);
            data.RunTime = span;
            data.EstimatedEndTime = endSpan;
            _ = callback(data);
        }

#if NETSTANDARD2_0

        private async Task InvokeJobInner(string id, DateTime? nowOverrideValue, Dictionary<string, string> data, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
#else
        private async Task InvokeJobInner(string id, DateTime? nowOverrideValue, Dictionary<string, string?>? data, TimeSpan? timeout = null,CancellationToken cancellationToken = default)
#endif
        {
            var restRequest = new RestRequest("job/invoke", HttpMethod.Post)
                .AddBody(new
                {
                    id,
                    nowOverrideValue,
                    data,
                    timeout
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        // bug fix: test finish while job still running
        private async Task<bool> IsHistoryStatusRunning(long logId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("history/{id}/status", HttpMethod.Get)
                .AddSegmentParameter("id", logId);

            try
            {
                var result = await _proxy.InvokeScalarAsync<int>(restRequest, cancellationToken);
                return result == -1;
            }
            catch
            {
                return true;
            }
        }

#if NETSTANDARD2_0

        private async Task<(RunningJobDetails, DateTime?)> LongPollingGetRunningData(
            Func<RunningJobDetails, Task> callback,
            RunningJobDetails data,
            string instanceId,
            DateTime invokeDate,
            CancellationToken cancellationToken)
#else
        private async Task<(RunningJobDetails?, DateTime?)> LongPollingGetRunningData(
            Func<RunningJobDetails, Task> callback,
            RunningJobDetails? data,
            string instanceId,
            DateTime invokeDate,
            CancellationToken cancellationToken)
#endif
        {
            var restRequest = new RestRequest("job/running-instance/{instanceId}/long-polling", HttpMethod.Get)
                .AddSegmentParameter("instanceId", instanceId)
                .AddQueryParameter("progress", data?.Progress ?? 0)
                .AddQueryParameter("effectedRows", data?.EffectedRows ?? 0)
                .AddQueryParameter("exceptionsCount", data?.ExceptionsCount ?? 0)
                .SetTimeoutSeconds(360); // 6 min

            var counter = 1;
            while (counter <= 3)
            {
                try
                {
                    data = await _proxy.InvokeAsync<RunningJobDetails>(restRequest, cancellationToken);
                    break;
                }
                catch (PlanarValidationException)
                {
                    return (null, null);
                }
                catch (PlanarNotFoundException)
                {
                    return (null, null);
                }
                ////catch (PlanarRequestTimeoutException)
                ////{
                ////    return (null, null);
                ////}
                catch (Exception)
                {
                    await Task.Delay(500 + ((counter - 1) ^ 2) * 500, cancellationToken);
                    counter++;
                }
            }

            var estimateEnd = GetEstimatedEndTime(data);
            InvokeCallback(callback, data, invokeDate, estimateEnd);

            return (data, estimateEnd);
        }
    }
}