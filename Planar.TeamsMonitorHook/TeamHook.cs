using Planar.MonitorHook;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.TeamsMonitorHook
{
    public class TeamHook : BaseMonitorHook
    {
        public override async Task Handle(IMonitorDetails monitorDetails)
        {
            var valid = ValidateGroup(monitorDetails.Group);
            if (valid == false) { return; }

            var message = GetMessageText(monitorDetails);
            await SendMessageToChannel(monitorDetails.Group.Reference1, message);
        }

        private bool ValidateGroup(IMonitorGroup group)
        {
            if (string.IsNullOrEmpty(group.Reference1))
            {
                LogError(null, "Group {Name} is invalid for Teams monitor hook. Reference1 is null or empty", group.Name);
                return false;
            }

            return true;
        }

        private async Task SendMessageToChannel(string url, string message)
        {
            if (string.IsNullOrEmpty(url)) { return; }

            try
            {
                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(new HttpMethod("POST"), url);
                var tempObject = new { text = message };
                var json = JsonSerializer.Serialize(tempObject);
                request.Content = new StringContent(json);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode == false)
                {
                    LogError(null, "Send message to url '{Url}' fail with status code {StatusCode}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Send message to url '{Url}' fail with error: {Message}", url, ex.Message);
            }
        }

        private static string GetMessageText(IMonitorDetails details)
        {
            var template = GetTemplate();
            template = Replace(template, "EventTitle", details.EventTitle);
            template = Replace(template, "JobGroup", details.JobGroup);
            template = Replace(template, "JobName", details.JobName);
            template = Replace(template, "JobDescription", details.JobDescription);
            template = Replace(template, "FireTime", $"{details.FireTime:g}");
            template = Replace(template, "JobRunTime", $"{details.JobRunTime:hh\\:mm\\:ss}");
            template = Replace(template, "JobId", details.JobId);
            template = Replace(template, "FireInstanceId", details.FireInstanceId);
            template = Replace(template, "Exception", details.Exception.Message);

            return template;
        }

        private static string Replace(string source, string find, string value)
        {
            return source.Replace($"@@{find}@@", value);
        }

        private static string GetTemplate()
        {
            var assembly = typeof(TeamHook).Assembly;
            var resourceName = "Planar.TeamsMonitorHook.MessageCard.json";

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