using Planar.MonitorHook;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
                // TODO: implement with message broker
                // _logger.LogError("Group {Name} is invalid for Teams monitor hook. All reference fields are empty. At least 1 reference field should have teams channel url", group.Name);
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
                    // TODO: implement with message broker
                    // _logger.LogError("Send message to url '{Url}' fail with status code {StatusCode}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // TODO: implement with message broker
                // _logger.LogError(ex, "Send message to url '{Url}' fail with error: {Message}", url, ex.Message);
            }
        }

        private string GetMessageText(IMonitorDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Job: {details.JobGroup}.{details.JobName}  ");
            sb.AppendLine($"Event: {details.EventTitle}  ");

            if (details.EventId == 6)
            {
                var status = details.Exception == null ? "Success" : "Fail";
                sb.AppendLine($"Status: {status}  ");
            }

            sb.AppendLine($"JobDescription: {details.JobDescription}  ");
            sb.AppendLine($"FireTime: {details.FireTime:g}  ");
            sb.AppendLine($"JobRunTime: {details.JobRunTime:hh\\:mm\\:ss}  ");
            sb.AppendLine($"FireInstanceId: {details.FireInstanceId}");
            if (details.Exception != null)
            {
                var ex = GetMostInnerException(details.Exception);
                sb.AppendLine($"  \r\nException: {ex.Message}");
            }

            return sb.ToString();
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