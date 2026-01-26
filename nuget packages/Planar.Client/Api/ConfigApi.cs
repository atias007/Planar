using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class ConfigApi : BaseApi, IConfigApi
    {
        public ConfigApi(RestProxy proxy) : base(proxy)
        {
        }

#if NETSTANDARD2_0

        public async Task AddAsync(string key, string value, string sourceUrl = null, ConfigType? configType = null, CancellationToken cancellationToken = default)
#else
        public async Task AddAsync(string key, string? value, string? sourceUrl = null, ConfigType? configType = null, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(key, nameof(key));

            var data = new { key, value, sourceUrl, Type = configType?.ToString().ToLower() };
            var restRequest = new RestRequest("config", HttpMethod.Post)
                    .AddBody(data);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(key, nameof(key));

            var restRequest = new RestRequest("config/{key}", HttpMethod.Delete)
                .AddSegmentParameter("key", key);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config/flush", HttpMethod.Post);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<GlobalConfig> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(key, nameof(key));
            var restRequest = new RestRequest("config/{key}", HttpMethod.Get)
                            .AddSegmentParameter("key", key);
            var result = await _proxy.InvokeAsync<GlobalConfig>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<GlobalConfig>> ListAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<GlobalConfig>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<KeyValueItem>> ListFlatAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config/flat", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<KeyValueItem>>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task UpdateAsync(string key, string value, string sourceUrl = null, ConfigType? configType = null, CancellationToken cancellationToken = default)
#else
        public async Task UpdateAsync(string key, string? value, string? sourceUrl = null, ConfigType? configType = null, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(key, nameof(key));

            var data = new { key, value, sourceUrl, Type = configType?.ToString().ToLower() };
            var restRequest = new RestRequest("config", HttpMethod.Put)
                .AddBody(data);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}