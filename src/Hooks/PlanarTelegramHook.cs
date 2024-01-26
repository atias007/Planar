using Planar.Hook;

namespace Planar.Hooks
{
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
  telegram-chat-it:<your-chat-id>
-------------------------------------
""";

        public override Task Handle(IMonitorDetails monitorDetails)
        {
            // https://api.telegram.org/bot<TOKEN>/sendMessage?chat_id=<CHATID>&text=<MESSAGE>
            // token: 5574394171:AAErT6psb6210KpTl8xotKTl5PLIL-QtJQg
            // chat id: -1002028679199
            throw new NotImplementedException();
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            throw new NotImplementedException();
        }
    }
}