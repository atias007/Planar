using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Planar.Monitor.Hook;
using System.Text;

namespace Planar.Hooks
{
    public class PlanarRestHook : BaseHook
    {
        public override string Name => "Planar.Rest";

        private static readonly object Locker = new();
        private static HttpClient _sharedClient = null!;

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        static PlanarRestHook() => _jsonSerializerSettings.Converters.Add(new NewtonsoftTimeSpanConverter());

        public override async Task Handle(IMonitorDetails monitorDetails)
        {
            await InvokeRest(monitorDetails);
        }

        public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            await InvokeRest(monitorDetails);
        }

        private async Task InvokeRest(IMonitor detials)
        {
            const string key = "RestHookUrl";
            var url = GetHookParameter(key, detials);
            if (!IsValidUri(url))
            {
                LogError(exception: null, $"url '{url}' of parameter '{key}' is invalid");
                return;
            }

            var body = new MonitorPayload
            {
                Created = DateTime.Now,
                HookName = Name,
                Message = detials,
                SourceUrl = url!
            };

            var client = GetHttpClient();
            var method = new HttpMethod("Post");
            var restRequest = new HttpRequestMessage(method, url);
            var json = JsonConvert.SerializeObject(body, _jsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            restRequest.Content = content;

            try
            {
                HttpResponseMessage response = await client.SendAsync(restRequest);
                if (!response.IsSuccessStatusCode)
                {
                    var message = GetErrorMessage(response);
                    throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {message}");
                }
            }
            catch (Exception ex)
            {
                throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
            }
        }

        private static string GetErrorMessage(HttpResponseMessage response)
        {
            var uri = response.RequestMessage?.RequestUri;
            var message = $"REST call POST: {uri} has response status {response.StatusCode}. Reason phrase: {response.ReasonPhrase}";
            return message;
        }

        private static HttpClient GetHttpClient()
        {
            lock (Locker)
            {
                if (_sharedClient == null)
                {
                    var handler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(15) // Recreate every 15 minutes
                    };

                    _sharedClient = new HttpClient(handler);
                }

                return _sharedClient;
            }
        }
    }
}