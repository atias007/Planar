using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planner.MonitorHook;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Planner.TeamsMonitorHook
{
    public class TeamHook : IMonitorHook
    {
        private ILogger _logger;

        public async Task Handle(IMonitorDetails monitorDetails, ILogger logger)
        {
            _logger = logger;
            var valid = ValidateGroup(monitorDetails.Group);
            if (valid == false) { return; }

            var message = GetMessageText(monitorDetails);
            await Task.WhenAll(new Task[]
            {
                SendMessageToChannel(monitorDetails.Group.Reference1, message),
                SendMessageToChannel(monitorDetails.Group.Reference2, message),
                SendMessageToChannel(monitorDetails.Group.Reference3, message),
                SendMessageToChannel(monitorDetails.Group.Reference4, message),
                SendMessageToChannel(monitorDetails.Group.Reference5, message)
            });
        }

        private bool ValidateGroup(IMonitorGroup group)
        {
            if (string.IsNullOrEmpty(group.Reference1) &&
                string.IsNullOrEmpty(group.Reference2) &&
                string.IsNullOrEmpty(group.Reference3) &&
                string.IsNullOrEmpty(group.Reference4) &&
                string.IsNullOrEmpty(group.Reference5))
            {
                _logger.LogError($"Group {group.Name} is invalid for Teams monitor hook. All reference fields are empty. At least 1 reference field should have teams channel url");
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
                var json = JsonConvert.SerializeObject(tempObject);
                request.Content = new StringContent(json);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode == false)
                {
                    _logger.LogError($"Send message to url '{url}' fail with status code {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Send message to url '{url}' fail with error: {ex.Message}");
            }
        }

        private string GetMessageText(IMonitorDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Planner job '{details.JobGroup}.{details.JobName}' alert event {details.EventTitle}  ");
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
    }
}