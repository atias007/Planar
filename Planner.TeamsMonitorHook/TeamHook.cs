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
        public async Task Handle(IMonitorDetails monitorDetails)
        {
            var message = GetMessageText(monitorDetails);
            await SendMessageToChannel(monitorDetails.Group.Reference1, message);
            await SendMessageToChannel(monitorDetails.Group.Reference2, message);
            await SendMessageToChannel(monitorDetails.Group.Reference3, message);
            await SendMessageToChannel(monitorDetails.Group.Reference4, message);
            await SendMessageToChannel(monitorDetails.Group.Reference5, message);
        }

        private static async Task SendMessageToChannel(string url, string message)
        {
            if (string.IsNullOrEmpty(url)) { return; }

            using var httpClient = new HttpClient();
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
            {
                var tempObject = new { text = message };
                var json = JsonConvert.SerializeObject(tempObject);
                request.Content = new StringContent(json);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode == false)
                {
                    throw new ApplicationException($"Send message to url '{url}' fail with status code {response.StatusCode}");
                }
            }
        }

        private string GetMessageText(IMonitorDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Planner job '{details.JobGroup}.{details.JobName}' alert event {details.EventTitle}  |  ");
            sb.AppendLine($"FireTime: {details.FireTime:g}  |  ");
            sb.AppendLine($"JobRunTime: {details.JobRunTime:hh\\:mm\\:ss}  |  ");
            sb.AppendLine($"FireInstanceId: {details.FireInstanceId}");
            if (details.Exception != null)
            {
                var ex = GetMostInnerException(details.Exception);
                sb.AppendLine($"  |  Exception: {ex.Message}");
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