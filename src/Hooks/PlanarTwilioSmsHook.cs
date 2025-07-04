﻿using Planar.Common;
using Planar.Hook;
using System.Globalization;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Planar.Hooks;

public sealed class PlanarTwilioSmsHook : BaseSystemHook
{
    public override string Name => "Planar.Twilio.Sms";

    public override string Description =>
        """
This hook send SMS message via Twilio API server.
You can find the configuration of Twilio API server is in appsettings.yml (Data folder of Planar).
SMS will be sent to all users of the monitor group.
The phone number of the user is defined in the user profile at 'PhoneNumber' fields.
Hook will send the message to all valid phone numbers of the user.
""";

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var message = GetSmsMessage(monitorDetails);
        await SendSms(message, monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var message = GetSmsMessage(monitorDetails);
        await SendSms(message, monitorDetails);
    }

    private static string GetSmsMessage(IMonitorDetails monitorDetails)
    {
        var template =
        $"""
Planar Monitor Alert
Event: {monitorDetails.EventTitle}
Environment: {monitorDetails.Environment}
Job: {monitorDetails.JobGroup}.{monitorDetails.JobName}
Fire Time: {monitorDetails.FireTime.ToShortDateString()} at {monitorDetails.FireTime.ToShortTimeString()}
Job Run Time: {monitorDetails.JobRunTime.ToString("hh\\:mm\\:ss", CultureInfo.CurrentCulture)}
Fire Instance Id: {monitorDetails.FireInstanceId}
Author: {monitorDetails.Author}
""";

        if (!string.IsNullOrWhiteSpace(monitorDetails.MostInnerExceptionMessage))
        {
            template += $"Error Message: {monitorDetails.MostInnerExceptionMessage}";
        }

        return template;
    }

    private static string GetSmsMessage(IMonitorSystemDetails monitorDetails)
    {
        var template =
$"""
Planar Monitor Alert
Event: {monitorDetails.EventTitle}
Environment: {monitorDetails.Environment}
Message: {monitorDetails.Message}
""";

        if (!string.IsNullOrWhiteSpace(monitorDetails.MostInnerExceptionMessage))
        {
            template += $"Error Message: {monitorDetails.MostInnerExceptionMessage}";
        }

        return template;
    }

    private async Task SendSms(string message, IMonitor monitor)
    {
        var settings = AppSettings.Hooks.TwilioSms;

        if (string.IsNullOrWhiteSpace(settings.AccountSid))
        {
            LogError($"fail to get account sid for hook \"{Name}\"");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.AuthToken))
        {
            LogError($"fail to get auth token for hook \"{Name}\"");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.FromNumber))
        {
            LogError($"fail to get from number for hook \"{Name}\"");
            return;
        }

        TwilioClient.Init(settings.AccountSid, settings.AuthToken);

        var phones = monitor.Users.Select(p => p.PhoneNumber1)
            .Union(monitor.Users.Select(p => p.PhoneNumber2))
            .Union(monitor.Users.Select(p => p.PhoneNumber3))
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => $"{settings.DefaultPhonePrefix}{p}")
            .Where(p => long.TryParse(p, out _))
            .Distinct();

        if (!phones.Any())
        {
            LogError($"fail to get valid phones for hook \"{Name}\" with distribution group {monitor.Groups.First().Name}");
            return;
        }

        foreach (var phone in phones)
        {
            var result = await MessageResource.CreateAsync(
                        body: message,
                        from: new Twilio.Types.PhoneNumber(settings.FromNumber),
                        to: new Twilio.Types.PhoneNumber(phone)
                    );

            if (result.ErrorCode == null)
            {
                LogDebug($"Twilio sms message to {phone} send with id {result.Sid}");
            }
            else
            {
                LogWarning($"fail to send \"{Name}\" hook sms message to {phone} with error (code: {result.ErrorCode}) {result.ErrorMessage}");
            }
        }
    }
}