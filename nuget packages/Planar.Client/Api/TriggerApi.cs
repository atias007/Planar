using Planar.Client.Entities;
using Planar.Client.Exceptions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class TriggerApi : BaseApi, ITriggerApi
    {
        public TriggerApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{triggerId}", Method.Delete)
                .AddParameter("triggerId", id, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{id}/data/{key}", Method.Delete)
                        .AddParameter("id", id, ParameterType.UrlSegment)
                        .AddParameter("key", key, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<TriggerBasicDetails> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/{triggerId}", Method.Get)
                .AddParameter("triggerId", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<TriggerBasicDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<TriggerBasicDetails> ListAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));

            var restRequest = new RestRequest("trigger/{jobId}/by-job", Method.Get)
                .AddParameter("jobId", jobId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<TriggerBasicDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetCronDescriptionAsync(string expression, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(expression, nameof(expression));
            var restRequest = new RestRequest("trigger/cron", Method.Get)
                .AddQueryParameter("expression", expression);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<PausedTrigger>> GetPausedAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("trigger/paused", Method.Get);
            var result = await _proxy.InvokeAsync<List<PausedTrigger>>(restRequest, cancellationToken);
            return result;
        }

        public async Task PauseAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/pause", Method.Post)
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

            var restRequest = new RestRequest("trigger/data", Method.Post).AddBody(prm);
            try
            {
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
            catch (PlanarConflictException)
            {
                restRequest = new RestRequest("trigger/data", Method.Put).AddBody(prm);
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
        }

        public async Task ResumeAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("trigger/resume", Method.Post)
               .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateCronExpressionAsync(string id, string cronExpression, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(cronExpression, nameof(cronExpression));

            var restRequest = new RestRequest("trigger/cron-expression", Method.Patch)
               .AddBody(new { id, cronExpression });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateIntervalAsync(string id, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(interval, nameof(interval));

            var restRequest = new RestRequest("trigger/interval", Method.Patch)
              .AddBody(new { id, interval });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task UpdateTimeoutAsync(string id, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            ValidateMandatory(timeout, nameof(timeout));

            var restRequest = new RestRequest("trigger/timeout", Method.Patch)
              .AddBody(new { id, timeout });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ClearTimeoutAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("trigger/timeout", Method.Patch)
              .AddBody(new { id });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}