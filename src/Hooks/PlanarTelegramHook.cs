using Newtonsoft.Json.Linq;
using Planar.Common;
using Planar.Hook;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

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
            var message = GetMessage(monitorDetails);
            await SendMessage(monitorDetails, message);
        }

        public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            var message = GetMessage(monitorDetails);
            await SendMessage(monitorDetails, message);
        }

        private static string GetBotToken(IMonitor monitor)
        {
            var token = GetParameter("telegram-bot-token", monitor.Group);
            if (string.IsNullOrWhiteSpace(token))
            {
                token = AppSettings.Hooks.Telegram.BotToken; // 5574394171:AAErT6psb6210KpTl8xotKTl5PLIL-QtJQg
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new PlanarHookException("Telegram bot token is not defined.");
            }

            return token;
        }

        private static string GetChatId(IMonitor monitor)
        {
            var chatid = GetParameter("telegram-chat-id", monitor.Group);
            if (string.IsNullOrWhiteSpace(chatid))
            {
                chatid = AppSettings.Hooks.Telegram.ChatId; // -1002028679199
            }

            if (string.IsNullOrWhiteSpace(chatid))
            {
                throw new PlanarHookException("Telegram chat id is not defined.");
            }

            return chatid;
        }

        private async Task SendMessage(IMonitor monitor, string message)
        {
            var token = GetBotToken(monitor);
            var chatid = GetChatId(monitor);

            var entity = new
            {
                text = message,
                parse_mode = "Markdown",
                //disable_web_page_preview = false,
                //disable_notification = false,
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
                    { "accept", "application/json" }
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

        private static string GetMessage(IMonitorDetails monitor)
        {
            var template =
$"""
`Planar Monitor Alert`

*Event:* {monitor.EventTitle}
*Environment:* {monitor.Environment}
*Job:* {monitor.JobGroup}.{monitor.JobName}
*Fire Time:* {monitor.FireTime.ToShortDateString()} at {monitor.FireTime.ToShortTimeString()}
*Job Run Time:* {monitor.JobRunTime.ToString("hh\\:mm\\:ss", CultureInfo.CurrentCulture)}
*Fire Instance Id:* {monitor.FireInstanceId}
*Author:* {monitor.Author}
""";

            if (!string.IsNullOrWhiteSpace(monitor.MostInnerExceptionMessage))
                template +=
$"""

---------------
*Exception:*
---------------
{monitor.MostInnerExceptionMessage}
""";
            return template.Replace("_", "-");
        }

        private static string GetMessage(IMonitorSystemDetails monitor)
        {
            var template =
$"""
**`Planar Monitor Alert`**

*Event:* {monitor.EventTitle}
*Environment:* {monitor.Environment}
*Message:* {monitor.Message}
""";

            if (!string.IsNullOrWhiteSpace(monitor.MostInnerExceptionMessage))
                template +=
$"""

---------------
*Exception:*
---------------
{monitor.MostInnerExceptionMessage}
""";
            return template.Replace("_", "-");
        }
    }
}