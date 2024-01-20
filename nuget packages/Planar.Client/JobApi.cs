using Planar.Client.Entities;
using Planar.Client.Exceptions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class JobApi : BaseApi, IJobApi
    {
        public JobApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<string> AddAsync(string folder, string? jobFileName, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(folder, nameof(folder));
            var body = new
            {
                folder,
                jobFileName
            };

            var restRequest = new RestRequest("job/folder", Method.Post)
                .AddBody(body);

            var result = await _proxy.InvokeAsync<PlanarIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

        public async Task PutDataAsync(string id, string key, string? value, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(key, nameof(key));
            var prm1 = new
            {
                Id = id,
                DataKey = key,
                DataValue = value
            };

            var restRequest = new RestRequest("job/data", Method.Post).AddBody(prm1);
            try
            {
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
            catch (PlanarConflictException)
            {
                restRequest = new RestRequest("job/data", Method.Put).AddBody(prm1);
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
        }

        public async Task CancelRunningAsync(string fireInstanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(fireInstanceId, nameof(fireInstanceId));
            var restRequest = new RestRequest("job/cancel", Method.Post)
                .AddBody(new { fireInstanceId });
            await _proxy.InvokeAsync<JobDescription>(restRequest, cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter("id", id, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(key, nameof(key));
            var restRequest = new RestRequest("job/{id}/data/{key}", Method.Delete)
                       .AddParameter("id", id, ParameterType.UrlSegment)
                       .AddParameter("key", key, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<JobDescription> DescribeJobAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/info", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);
            var result = await _proxy.InvokeAsync<JobDescription>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<KeyValueItem>> GatSettingsAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/settings", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<IEnumerable<KeyValueItem>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IPagingResponse<JobAudit>> GetAllAuditsAsync(PagingRequest? paging, CancellationToken cancellationToken = default)
        {
            paging ??= new PagingRequest();
            paging.SetPagingDefaults();
            var restRequest = new RestRequest("job/audits", Method.Get)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<JobAudit>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<JobAuditWithInformation> GetAuditAsync(int auditId, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/audit/{auditId}", Method.Get)
                .AddParameter("auditId", auditId, ParameterType.UrlSegment);
            var result = await _proxy.InvokeAsync<JobAuditWithInformation>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IPagingResponse<JobAudit>> GetAuditsAsync(string id, PagingRequest? paging, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/audit", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);
            var result = await _proxy.InvokeAsync<PagingResponse<JobAudit>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<JobDetails> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<JobDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetJobFileAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            var restRequest = new RestRequest("job/jobfile/{name}", Method.Get)
                .AddUrlSegment("name", name);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<string>> GetJobTypesAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/types", Method.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<string>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<DateTime?> GetNextRunningAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/next-running", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<DateTime?>(restRequest, cancellationToken);
            return result;
        }

        public async Task<DateTime?> GetPreviousRunningAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/{id}/prev-running", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<DateTime?>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<RunningJobDetails>> GetRunningAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/running", Method.Get);
            var result = await _proxy.InvokeAsync<List<RunningJobDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<RunningJobDetails> GetRunningAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("job/running-instance/{instanceId}", Method.Get)
                    .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
            var result = await _proxy.InvokeAsync<RunningJobDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<RunningJobData> GetRunningDataAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("job/running-data/{instanceId}", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<RunningJobData>(restRequest, cancellationToken);
            return result;
        }

        public async Task InvokeAsync(string id, DateTime? nowOverrideValue = null, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var request = new
            {
                id,
                nowOverrideValue,
                data
            };

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(request);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<IPagingResponse<JobBasicDetails>> ListAsync(JobsFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var f = filter ?? JobsFilter.Empty;
            var restRequest = new RestRequest("job", Method.Get);
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

        public async Task PauseAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<string> QueueInvokeAsync(
            string id,
            DateTime dueDate,
            TimeSpan? timeout,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
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

            var restRequest = new RestRequest("job/queue-invoke", Method.Post)
                .AddBody(request);
            var result = await _proxy.InvokeAsync<PlanarIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

        public async Task ResumeAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/resume", Method.Post)
               .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
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

            var restRequest = new RestRequest("job", Method.Put)
                .AddBody(body);

            var result = await _proxy.InvokeAsync<PlanarIdResponse>(restRequest, cancellationToken);
            return result.Id;
        }

        public async Task SetAuthor(string id, string author, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(author, nameof(author));
            var restRequest = new RestRequest("job/author", Method.Patch)
                .AddBody(new
                {
                    id,
                    author
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<TestStatus> TestAsync(string id, Action<RunningJobDetails> callback, DateTime? nowOverrideValue = null, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var invokeDate = DateTime.Now.AddSeconds(-1);

            // (0) Check the job
            await CheckAlreadyRunningJobInner(id, cancellationToken);

            // (1) Invoke job
            await InvokeJobInner(id, nowOverrideValue, data, cancellationToken);

            // (2) Sleep 1 sec
            await Task.Delay(1000, cancellationToken);

            // (3) Get last instance id
            var lastInstanceId = await GetLastInstanceId(id, invokeDate, cancellationToken);
            var instanceId = lastInstanceId.InstanceId;
            var logId = lastInstanceId.LogId;

            // (4) Get running info

            // (5) Sleep 1 sec
            await Task.Delay(1000, cancellationToken);
        }

        private async Task CheckAlreadyRunningJobInner(string id, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running", Method.Get);
            var result = await _proxy.InvokeAsync<List<RunningJobDetails>>(restRequest, cancellationToken);

            var exists = result.Exists(d => d.Id == id || string.Equals($"{d.Group}.{d.Name}", id, StringComparison.OrdinalIgnoreCase));
            if (exists) { throw new PlanarException($"job id {id} already running. test can not be invoked until job done"); }
        }

        private async Task InvokeJobInner(string id, DateTime? nowOverrideValue, Dictionary<string, string>? data, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(new
                {
                    id,
                    nowOverrideValue,
                    data
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        private async Task<LastInstanceId> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/last-instance-id/long-polling", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", dateParameter, ParameterType.QueryString);

            restRequest.Timeout = 35_000; // 35 sec

            try
            {
                var result = await _proxy.InvokeAsync<LastInstanceId?>(restRequest, cancellationToken)
                    ?? throw new PlanarException("could not found running instance id. check whether job is paused or maybe another instance already running");

                return result;
            }
            catch (PlanarConflictException)
            {
                throw new PlanarConflictException($"job id {id} already running. test can not be invoked until job done");
            }
        }
    }
}