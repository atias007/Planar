using Planar.Client.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class JobApi : BaseApi, IJobApi
    {
        public JobApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<string> AddAsync(string folder, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> AddAsync(string folder, string jobFileName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task AddDataAsync(string id, string key, string value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task CancelRunningAsync(string fireInstanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            throw new NotImplementedException();
        }

        public async Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public async Task<RunningJobData> GetRunningDataAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<RunningJobDetails> GetRunningInfoAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task InvokeAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task InvokeAsync(string id, DateTime nowOverrideValue, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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

        public async Task QueueInvokeAsync(string id, DateTime dueDate, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            throw new NotImplementedException();
        }

        public async Task QueueInvokeAsync(string id, DateTime dueDate, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            throw new NotImplementedException();
        }

        public async Task ResumeAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("job/resume", Method.Post)
               .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<string> UpdateAsync(string folder, bool updateJobData = true, bool updateTriggerData = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UpdateAsync(string folder, string jobFileName, bool updateJobData = true, bool updateTriggerData = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateJobDataAsync(string id, string key, string value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}