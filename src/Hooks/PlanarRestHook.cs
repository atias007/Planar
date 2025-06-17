using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Core.JsonConvertors;
using Planar.Common;
using Planar.Hook;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Planar.Hooks;

public sealed class PlanarRestHook : BaseSystemHook
{
    public override string Name => "Planar.Rest";

    public override string Description =>
"""
This hook send a REST HTTP POST request with application/json body content type.
You can find the default url of the request is in appsettings.yml (Data folder of Planar).
To use different url per group, you can set one of the 'AdditionalField' of monitor group to the following value:
----------------------------------------------
  rest-http-url:<http://your-server/your-path>
----------------------------------------------
""";

    private static readonly Lock Locker = new();
    private static HttpClient _sharedClient = null!;

    private static readonly JsonSerializerOptions _jsonSerializerSettings = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new SystemTextTimeSpanConverter(),
            new SystemTextNullableTimeSpanConverter()
        }
    };

    private static readonly JsonEventFormatter _formatter = new(
        serializerOptions: _jsonSerializerSettings,
         documentOptions: default
        );

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var urls = GetUrls(monitorDetails);
        foreach (var url in urls)
        {
            await InvokeRest(monitorDetails, url);
        }
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var urls = GetUrls(monitorDetails);
        foreach (var url in urls)
        {
            await InvokeRest(monitorDetails, url);
        }
    }

    private List<string> GetUrls(IMonitor monitor)
    {
        var urls = new List<string>();
        foreach (var group in monitor.Groups)
        {
            var url = GetParameter("rest-http-url", group);
            if (string.IsNullOrWhiteSpace(url))
            {
                url = AppSettings.Hooks.Rest.DefaultUrl;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                LogError("Rest.Hook: url is null or empty");
                continue;
            }

            if (!IsValidUri(url))
            {
                LogError($"url '{url}' of rest hook is invalid");
                continue;
            }

            urls.Add(url);
        }

        return urls;
    }

    private async Task InvokeRest<T>(T detials, string url)
        where T : IMonitor
    {
        var body = new CloudEvent
        {
            Id = Guid.NewGuid().ToString("N"),
            Time = DateTimeOffset.Now,
            Subject = Name,
            Data = detials,
            DataContentType = MediaTypeNames.Application.Json,
            Source = new Uri(url),
            Type = typeof(T).Name
        };

        body.SetAttributeFromString("version", "1.0.0");

        var client = GetHttpClient();
        var method = new HttpMethod("Post");

        using var restRequest = new HttpRequestMessage(method, url);
        var bytes = _formatter.EncodeStructuredModeMessage(body, out _);
        var json = Encoding.UTF8.GetString(bytes.Span);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        restRequest.Content = content;

        try
        {
            using HttpResponseMessage response = await client.SendAsync(restRequest);
            if (response.IsSuccessStatusCode)
            {
                LogInformation($"Rest hook response success status code: {response.StatusCode}");
            }
            else
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