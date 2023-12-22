using MessageBird;
using MessageBird.Objects;
using Planar.Hook;

namespace Planar.MonitorHook.MessageBirdSms
{
    internal class MessageBirdSmsHook : BaseHook
    {
        public override string Name => "MessageBirdSms";

        private const string MessageBirdSmsAccessKey = "MessageBirdSmsAccessKey";
        private const string MessageBirdSmsFrom = "MessageBirdSmsFrom";
        private const string MessageBirdCountryCode = "MessageBirdCountryCode";

        public override Task Handle(IMonitorDetails monitorDetails)
        {
            var message = $"{monitorDetails.EventTitle}: For job: {monitorDetails.JobGroup}.{monitorDetails.JobName} ({monitorDetails.JobId})" +
                $"\r\nAuthor: {monitorDetails.Author}" +
                $"\r\nFireTime: {monitorDetails.FireTime:g}";

            if (monitorDetails.Exception != null)
            {
                message += $"\r\nError: {monitorDetails.MostInnerExceptionMessage}";
            }

            SendMessage(message, monitorDetails);
            return Task.CompletedTask;
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            var message = $"{monitorDetails.EventTitle}: {monitorDetails.Message}" + $"\r\nFireTime: {DateTime.Now:g}";
            SendMessage(message, monitorDetails);
            return Task.CompletedTask;
        }

        private void SendMessage(string message, IMonitor monitor)
        {
            var accessKey = GetHookParameter(MessageBirdSmsAccessKey, monitor);
            var from = GetHookParameter(MessageBirdSmsFrom, monitor);
            var prefix = GetHookParameter(MessageBirdCountryCode, monitor);

            if (string.IsNullOrEmpty(from))
            {
                from = nameof(Planar);
            }

            if (!ValidatePrefix(prefix)) { return; }
            if (!ValidateAccessKey(accessKey)) { return; }
            if (accessKey == null) { return; }

            var phones = monitor.Users.Select(p => p.PhoneNumber1)
                .Union(monitor.Users.Select(p => p.PhoneNumber2))
                .Union(monitor.Users.Select(p => p.PhoneNumber3))
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => $"{prefix}{p}")
                .Where(p => long.TryParse(p, out _))
                .Select(long.Parse);

            if (!ValidatePhones(phones, monitor)) { return; }

            try
            {
                var client = Client.CreateDefault(accessKey);
                var result = client.SendMessage(from, message, phones.ToArray(), new MessageOptionalArguments { Encoding = DataEncoding.Unicode });

                foreach (var item in result.Recipients.Items)
                {
                    if (item.Status != Recipient.RecipientStatus.Sent)
                    {
                        LogError($"sms message to {item.Msisdn} fail with status {item.Status}");
                    }
                }

                LogInformation("MessageBirdSms SMS send");
            }
            catch (Exception ex)
            {
                LogError($"fail to send sms message from {nameof(MessageBirdSmsHook)}. Error: {ex.Message}");
            }
        }

        private static bool ValidatePrefix(string? prefix)
        {
            if (string.IsNullOrEmpty(prefix)) { return true; }
            return true;
        }

        private bool ValidateAccessKey(string? accessKey)
        {
            if (string.IsNullOrEmpty(accessKey))
            {
                LogError($"fail to get access key parameter for {nameof(MessageBirdSmsHook)}");
                return false;
            }

            return true;
        }

        private bool ValidatePhones(IEnumerable<long> phones, IMonitor monitor)
        {
            if (!phones.Any())
            {
                LogError($"fail to get valid phones for {nameof(MessageBirdSmsHook)} with distribution group {monitor.Group.Name}");
                return false;
            }

            return true;
        }
    }
}