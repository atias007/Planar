using Planar.Client.Entities;
using Planar.Client.Exceptions;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class TriggerApi : BaseApi, ITriggerApi
    {
        public TriggerApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task ClearDataAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{id}/data", HttpMethod.Delete)
                        .AddSegmentParameter("id", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ClearTimeoutAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("trigger/timeout", HttpPatchMethod)
              .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{triggerId}", HttpMethod.Delete)
                .AddSegmentParameter("triggerId", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{id}/data/{key}", HttpMethod.Delete)
                        .AddSegmentParameter("id", id)
                        .AddSegmentParameter("key", key);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<TriggerBasicDetails> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{triggerId}", HttpMethod.Get)
                .AddSegmentParameter("triggerId", id);

            var result = await _proxy.InvokeAsync<TriggerBasicDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetCronDescriptionAsync(string expression, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(expression, nameof(expression));
            var restRequest = new RestRequest("trigger/cron", HttpMethod.Get)
                .AddQueryParameter("expression", expression);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<PausedTrigger>> GetPausedAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("trigger/paused", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<PausedTrigger>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<TriggerBasicDetails> ListAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));

            var restRequest = new RestRequest("trigger/{jobId}/by-job", HttpMethod.Get)
                .AddSegmentParameter("jobId", jobId);

            var result = await _proxy.InvokeAsync<TriggerBasicDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task PauseAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/pause", HttpMethod.Post)
               .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task PutDataAsync(string id, string key, string value, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(key, nameof(key));

            var prm = new
            {
                Id = id,
                DataKey = key,
                DataValue = value
            };

            var restRequest = new RestRequest("trigger/data", HttpMethod.Post).AddBody(prm);
            try
            {
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
            catch (PlanarConflictException)
            {
                restRequest = new RestRequest("trigger/data", HttpMethod.Put).AddBody(prm);
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
        }

        public async Task ResumeAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/resume", HttpMethod.Post)
               .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateCronExpressionAsync(string id, string cronExpression, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(cronExpression, nameof(cronExpression));

            var restRequest = new RestRequest("trigger/cron-expression", HttpPatchMethod)
               .AddBody(new { id, cronExpression });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateIntervalAsync(string id, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(interval, nameof(interval));

            var restRequest = new RestRequest("trigger/interval", HttpPatchMethod)
              .AddBody(new { id, interval });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateTimeoutAsync(string id, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(timeout, nameof(timeout));

            var restRequest = new RestRequest("trigger/timeout", HttpPatchMethod)
              .AddBody(new { id, timeout });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}