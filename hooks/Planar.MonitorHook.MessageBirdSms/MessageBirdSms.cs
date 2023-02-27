using MessageBird;
using MessageBird.Objects;
using Planar.Monitor.Hook;

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
                message += $"\r\nError: {monitorDetails.Exception.Message}";
            }

            SendMessage(message, monitorDetails);
            return Task.CompletedTask;
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            var message = $"{monitorDetails.EventTitle}: {monitorDetails.Message}" + $"\r\nFireTime: {DateTime.Now:g}";

            if (monitorDetails.Exception != null)
            {
                //message += $" Error: {monitorDetails.Exception.Message}";
            }

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

            var phones = monitor.Users.Select(p => p.PhoneNumber1)
                .Union(monitor.Users.Select(p => p.PhoneNumber2))
                .Union(monitor.Users.Select(p => p.PhoneNumber3))
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => $"{prefix}{p}")
                .Where(p => long.TryParse(p, out _))
                .Select(p => long.Parse(p));

            if (!ValidatePhones(phones, monitor)) { return; }

            try
            {
                var client = Client.CreateDefault(accessKey);
                var result = client.SendMessage(from, message, phones.ToArray(), new MessageOptionalArguments { Encoding = DataEncoding.Unicode });

                foreach (var item in result.Recipients.Items)
                {
                    if (item.Status != MessageBird.Objects.Recipient.RecipientStatus.Sent)
                    {
                        LogError(null, "sms message to {Phone} fail with status {Status}", item.Msisdn, item.Status.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "fail to send sms message from {Hook}. Error: {Error}", nameof(MessageBirdSmsHook), ex.Message);
            }
        }

        private bool ValidatePrefix(string? prefix)
        {
            if (string.IsNullOrEmpty(prefix)) { return true; }
            return true;
        }

        private bool ValidateAccessKey(string? accessKey)
        {
            if (string.IsNullOrEmpty(accessKey))
            {
                LogError(null, "fail to get access key parameter for {Hook}", nameof(MessageBirdSmsHook));
                return false;
            }

            return true;
        }

        private bool ValidatePhones(IEnumerable<long> phones, IMonitor monitor)
        {
            if (!phones.Any())
            {
                LogError(null, "fail to get valid phones for {Hook} with distribution group {Group}", nameof(MessageBirdSmsHook), monitor.Group.Id);
                return false;
            }

            return true;
        }
    }
}