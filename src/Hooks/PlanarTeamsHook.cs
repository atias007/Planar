using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Enities;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Planar.Hooks;

public sealed class PlanarTeamsHook : BaseSystemHook
{
    public override string Name => "Planar.Teams";

    public override string Description =>
    """
This hook send message to Teams channel via microsoft Teams server.
You can find the configuration of Teams server is in appsettings.yml (Data folder of Planar).
The configuration also define the default channel address.
To use different channel address per group, you can set one of the 'AdditionalField' of monitor group to the following value:
-------------------------------------------------
  teams-channel-address:<http://your-channel-url>
-------------------------------------------------
To send to multiple channels, you can set the following value (in appsettings.yml, teams section) to true:
-----------------------------
  send to multiple urls: true
-----------------------------
""";

    private const string ImageSource = "https://raw.githubusercontent.com/atias007/Planar/master/hooks/Planar.TeamsMonitorHook/Icons/{0}.png";

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var urls = GetTeamsUrls(monitorDetails);
        var message = GetMessageText(monitorDetails);
        foreach (var url in urls)
        {
            await SendMessageToChannel(url, message);
            if (!AppSettings.Hooks.Teams.SendToMultipleUrls) { break; }
        }
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var urls = GetTeamsUrls(monitorDetails);
        var message = GetSystemMessageText(monitorDetails);
        foreach (var url in urls)
        {
            await SendMessageToChannel(url, message);
            if (!AppSettings.Hooks.Teams.SendToMultipleUrls) { break; }
        }
    }

    private async Task SendMessageToChannel(string url, string json)
    {
        if (string.IsNullOrEmpty(url)) { return; }

        try
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"), url);
            request.Content = new StringContent(json);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                LogError($"Send message to Teams hook channel at url '{url}' fail with status code {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            LogError($"Send message to Teams hook channel at url '{url}' fail with error: {ex.Message}");
        }
    }

    private static string GetMessageText(IMonitorDetails details)
    {
        var runtime = details.JobRunTime.TotalHours < 24 ?
            $"{details.JobRunTime:hh\\:mm\\:ss}" :
            $"{details.JobRunTime:\\(d\\)\\ hh\\:mm\\:ss}";

        var icon = GetIcon(details);
        var image = string.Format(ImageSource, icon);

        var card = new TeamsMessageCard
        {
            Title = $"Monitor Event: {details.EventTitle}",
            Sections = new List<Section>
            {
                new Section
                {
                    ActivityTitle = $"For job: {details.JobGroup}.{details.JobName}",
                    ActivitySubtitle = details.JobDescription,
                    ActivityImage = image,
                    Text = details.MostInnerExceptionMessage ?? string.Empty,
                    Facts = new List<Fact>
                    {
                        new Fact("Fire time:", $"{details.FireTime:g}"),
                        new Fact("Run Time:", runtime),
                        new Fact("Job id:", details.JobId)    ,
                        new Fact("Fire instance id:", details.FireInstanceId)
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return json;
    }

    private static string GetSystemMessageText(IMonitorSystemDetails details)
    {
        var icon = GetIcon(details);
        var image = string.Format(ImageSource, icon);

        var card = new TeamsMessageCard
        {
            Title = $"Planar Event: {details.EventTitle}",
            Sections = new List<Section>
            {
                new Section
                {
                    ActivityTitle = $"System event occur at:",
                    ActivitySubtitle = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    ActivityImage = image,
                    Text= details.Message,
                    Facts = details.MessagesParameters
                        .Select(i => new Fact($"{i.Key}:", i.Value))
                        .ToList()
                }
            }
        };

        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return json;
    }

    private static string GetIcon(IMonitor details)
    {
        return details.EventId switch
        {
            100 => "veto",
            101 => "retry",
            102 => "fail",
            103 => "success",
            104 => "start",
            105 => "end",
            106 => "warn",
            200 => "fail",
            201 => "fail",
            202 => "fail",
            203 => "fail",
            300 => "info",
            301 => "warn",
            302 => "warn",
            303 => "warn",
            304 => "info",
            305 => "warn",
            306 => "info",
            307 => "fail",
            308 => "warn",
            309 => "info",
            310 => "fail",
            311 => "warn",
            312 => "info",
            _ => "info",
        };
    }

    private IEnumerable<string> GetTeamsUrls(IMonitor monitor)
    {
        var g = monitor.Group;
        var fields = new[]
        {
            g.AdditionalField1,
            g.AdditionalField2,
            g.AdditionalField3,
            g.AdditionalField4,
            g.AdditionalField5,
            AppSettings.Hooks.Teams.DefaultUrl
        };

        foreach (var item in fields)
        {
            var url = GetTeamsUrl(item);
            if (url != null) { yield return url; }
            if (!AppSettings.Hooks.Teams.SendToMultipleUrls) { break; }
        }
    }

    private string? GetTeamsUrl(string? additionalField)
    {
        var url = GetParameter("teams-channel-address", additionalField);
        if (url == null) { return null; }

        if (!IsValidUri(url))
        {
            LogError($"url '{url}' of teams hook is invalid");
            return null;
        }

        return url;
    }
}