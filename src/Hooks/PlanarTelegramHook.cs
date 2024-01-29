using Newtonsoft.Json.Linq;
using Planar.Hook;
using Planar.Hooks.General;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Web;

namespace Planar.Hooks
{
    // https://telegram-bot-sdk.readme.io/reference/sendmessage
    public sealed class PlanarTelegramHook : BaseSystemHook
    {
        public override string Name => "Planar.Telegram";

        public override string Description =>
"""
This hook send message via bot to chat using Telegram API.
You can find the configuration of Telegram is in appsettings.yml (Data folder of Planar).
The configuration define the default bot access token and chat id.
To use different bot token/chat id per group, you can set one of the 'AdditionalField' of monitor group to the following value:
-------------------------------------
  telegram-bot-token:<your-bot-token>
  telegram-chat-id:<your-chat-id>
-------------------------------------
""";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Telegram URL")]
        private const string telegramUrl = "https://api.telegram.org/bot{0}/sendMessage";

        public override async Task Handle(IMonitorDetails monitorDetails)
        {
            await SendMessage(monitorDetails, string.Empty);
        }

        public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            await SendMessage(monitorDetails, string.Empty);
        }

        private async Task SendMessage(IMonitor monitor, string message)
        {
            var token = GetParameter("telegram-bot-token", monitor.Group); // 5574394171:AAErT6psb6210KpTl8xotKTl5PLIL-QtJQg
            var chatid = GetParameter("telegram-chat-id", monitor.Group);  // -1002028679199

            var entity = new
            {
                text = message,
                parse_mode = "Markdown",
                disable_web_page_preview = false,
                disable_notification = false,
                chat_id = chatid
            };
            var json = JsonSerializer.Serialize(entity);

            var url = string.Format(telegramUrl, token);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { "accept", "application/json" },
                    { "User-Agent", "Planar: Telegram Bot SDK" },
                },
                Content = new StringContent(json)
                {
                    Headers =
                    {
                        ContentType =  new MediaTypeHeaderValue(MediaTypeNames.Application.Json)
                    }
                }
            };

            using var client = new HttpClient();
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var jtoken = JToken.Parse(body);
            var ok = jtoken.Value<bool>("ok");
            if (ok)
            {
                LogInformation($"Telegram response Ok with message id {jtoken["result"]?["message_id"]}");
            }
            else
            {
                LogError($"Telegram response error: {jtoken["description"]}");
            }
        }
    }
}