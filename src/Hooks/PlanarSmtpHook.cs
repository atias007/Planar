using MimeKit;
using Planar.Hook;
using Planar.Hooks.EmailTemplates;

namespace Planar.Hooks;

public sealed class PlanarSmtpHook : BaseSystemHook
{
    public override string Name => "Planar.Smtp";

    public override string Description =>
"""
This hook send email message via SMTP server.
You can find the configuration of SMTP server is in appsettings.yml (Data folder of Planar).
Email will be sent to all users of the monitor group.
The email address of the user is defined in the user profile at 'EmailAddress' fields.
Hook will send the message to all valid email addresses of the user.
""";

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var html = EmailHtmlGenerator.GetEmailHtml(monitorDetails);
        await Handle(html, monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var html = EmailHtmlGenerator.GetEmailHtml(monitorDetails);
        await Handle(html, monitorDetails);
    }

    private async Task Handle(string html, IMonitor monitor)
    {
        var message = new MimeMessage();
        var emails = GetEmails(monitor);
        foreach (var recipient in emails)
        {
            message.Bcc.Add(new MailboxAddress(recipient, recipient));
        }

        message.Subject = $"Planar Alert. Name: [{monitor.MonitorTitle}], Event: [{monitor.EventTitle}]";
        var body = new BodyBuilder
        {
            HtmlBody = html
        }.ToMessageBody();

        message.Body = body;

        var result = await SmtpUtil.SendMessage(message);
        LogInformation($"SMTP send result: {result}");
    }

    private static IEnumerable<string> GetEmails(IMonitor monitor)
    {
        var emails1 = monitor.Users.Select(u => u.EmailAddress1);
        var emails2 = monitor.Users.Select(u => u.EmailAddress2);
        var emails3 = monitor.Users.Select(u => u.EmailAddress3);
        var allEmails = emails1.Concat(emails2).Concat(emails3)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x ?? string.Empty)
            .Distinct();

        return allEmails;
    }
}