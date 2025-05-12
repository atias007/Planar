using Planar.Client.Entities;
using Planar.Client.Exceptions;
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

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(key, nameof(key));
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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

        public async Task PutAsync(string key, string value, ConfigType configType = ConfigType.String, CancellationToken cancellationToken = default)
#else
        public async Task PutAsync(string key, string? value, ConfigType configType = ConfigType.String, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(key, nameof(key));

            var data = new { key, value, Type = configType.ToString().ToLower() };
            try
            {
                var restRequest = new RestRequest("config", HttpMethod.Post)
                        .AddBody(data);
                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
            catch (PlanarNotFoundException)
            {
                var restRequest = new RestRequest("config", HttpMethod.Put)
                    .AddBody(data);

                await _proxy.InvokeAsync(restRequest, cancellationToken);
            }
        }
    }
}