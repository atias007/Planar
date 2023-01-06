using Planar.MonitorHook;
using Planar.TeamsMonitorHook.TeamsCards;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.TeamsMonitorHook
{
    // https://docs.microsoft.com/en-us/graph/api/chatmessage-post?view=graph-rest-1.0&tabs=powershell
    public class TeamHook : BaseMonitorHook
    {
        private const string ImageSource = "https://raw.githubusercontent.com/atias007/Planar/master/hooks/Planar.TeamsMonitorHook/Icons/{0}.png";

        public override async Task Handle(IMonitorDetails monitorDetails)
        {
            var valid = ValidateGroup(monitorDetails);
            if (!valid) { return; }

            var message = GetMessageText(monitorDetails);
            await SendMessageToChannel(monitorDetails.Group.Reference1, message);
        }

        public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            var valid = ValidateGroup(monitorDetails);
            if (!valid) { return; }

            var message = GetSystemMessageText(monitorDetails);
            await SendMessageToChannel(monitorDetails.Group.Reference1, message);
        }

        private bool ValidateGroup(IMonitor details)
        {
            if (string.IsNullOrEmpty(details.Group.Reference1))
            {
                LogError(null, "Group '{Name}' is invalid for Teams monitor hook. Reference1 is null or empty", details.Group.Name);
                return false;
            }

            if (!IsValidUri(details.Group.Reference1))
            {
                LogError(null, "Group '{Name}' has invalid uri value '{Reference1}' at 'Reference1' property", details.Group.Name, details.Group.Reference1);
                return false;
            }

            return true;
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
                    LogError(null, "Send message to Teams hook channel at url '{Url}' fail with status code {StatusCode}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Send message to Teams hook channel at url '{Url}' fail with error: {Message}", url, ex.Message);
            }
        }

        private static string GetMessageText(IMonitorDetails details)
        {
            var runtime = details.JobRunTime.TotalHours < 24 ?
                $"{details.JobRunTime:hh\\:mm\\:ss}" :
                $"{details.JobRunTime:\\(d\\)\\ hh\\:mm\\:ss}";

            var icon = GetIcon(details);
            var image = string.Format(ImageSource, icon);

            var card = new JobMessageCard
            {
                Title = $"Monitor Event: {details.EventTitle}",
                Sections = new List<Section>
                {
                    new Section
                    {
                        ActivityTitle = $"For job: {details.JobGroup}.{details.JobName}",
                        ActivitySubtitle = details.JobDescription,
                        ActivityImage = image,
                        Text= details.Exception?.Message,
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

            var card = new JobMessageCard
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
                1 => "veto",
                2 => "retry",
                3 => "fail",
                4 => "success",
                5 => "start",
                6 => "end",
                7 => "warn",
                100 => "fail",
                101 => "fail",
                102 => "fail",
                103 => "fail",
                200 => "info",
                _ => "info",
            };
        }

        private static string Replace(string source, string find, string value)
        {
            return source.Replace($"@@{find}@@", value);
        }

        private static string GetTemplate(string name)
        {
            var assembly = typeof(TeamHook).Assembly;
            var resourceName = $"Planar.TeamsMonitorHook.{name}.json";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        private Exception GetMostInnerException(Exception ex)
        {
            if (ex.InnerException == null) { return ex; }
            return GetMostInnerException(ex.InnerException);
        }

        public override Task Test(IMonitorDetails monitorDetails)
        {
            throw new NotImplementedException();
        }
    }
}