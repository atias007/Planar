using MimeKit;
using Planar.Hook;
using System.Text;
using System.Web;

namespace Planar.Hooks;

public class PlanarSmtpHook : BaseSystemHook
{
    public override string Name => "Planar.Smtp";

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var message = new MimeMessage();
        var emails1 = monitorDetails.Users.Select(u => u.EmailAddress1);
        var emails2 = monitorDetails.Users.Select(u => u.EmailAddress2);
        var emails3 = monitorDetails.Users.Select(u => u.EmailAddress3);
        var allEmails = emails1.Concat(emails2).Concat(emails3)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

        foreach (var recipient in allEmails)
        {
            message.To.Add(new MailboxAddress(recipient, recipient));
        }

        message.Subject = $"Planar Alert. Name: [{monitorDetails.MonitorTitle}], Event: [{monitorDetails.EventTitle}]";
        var body = new BodyBuilder
        {
            TextBody = GetText(monitorDetails),
            HtmlBody = GetHtml(monitorDetails),
        }.ToMessageBody();

        message.Body = body;

        var result = await SmtpUtil.SendMessage(message);
        LogDebug($"SMTP send result: {result}");
    }

    public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        throw new NotImplementedException();
    }

    private static string GetText(IMonitorDetails details)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Title: {details.MonitorTitle}");
        sb.AppendLine($"Event: {details.EventTitle}");
        sb.AppendLine($"Job Key: {details.JobGroup}.{details.JobName}");
        sb.AppendLine($"Job Description: {details.JobDescription}");
        sb.AppendLine($"Author: {details.Author}");
        sb.AppendLine($"Fire Instance Id: {details.FireInstanceId}");
        sb.AppendLine($"Fire Time: {details.FireTime}");
        sb.AppendLine($"Run Time: {FormatTimeSpan(details.JobRunTime)}");
        sb.AppendLine($"Trigger Name: {details.TriggerName}");

        if (!string.IsNullOrEmpty(details.Exception))
        {
            sb.AppendLine($"Error Message: {details.MostInnerExceptionMessage}");
            sb.AppendLine(string.Empty.PadLeft(40, '-'));
            sb.AppendLine($"Exception Details:");
            sb.AppendLine(details.Exception);
        }

        return sb.ToString();
    }

    public static string GetHtml(IMonitorDetails details)
    {
        var template = @"
<!DOCTYPE html>
<html>
<head>
<style>
body { direction: ltr; }
table {
  font-family: arial, sans-serif;
  border-collapse: collapse;
  direction: ltr;
}
p {
  font-family: arial, sans-serif;
  direction: ltr;
}
td, th {
  border: 1px solid #dddddd;
  text-align: left;
  padding: 8px;
}
.title {
    background-color: #dddddd
}
</style>
</head>
<body>

  {{TABLE}}

</body>
</html>
";

        var itempTemplate = @"
<tr>
    <td class=""title"">{{KEY}}</td>
    <td>{{VALUE}}</td>
</tr>";

        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Title").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.MonitorTitle)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Event").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.EventTitle)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Job Key").Replace("{{VALUE}}", HttpUtility.HtmlEncode($"{details.JobGroup}.{details.JobName}")));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Job Description").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.JobDescription)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Author").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.Author)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Fire Instance Id").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.FireInstanceId)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Fire Time").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.FireTime.ToString())));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Run Time").Replace("{{VALUE}}", FormatTimeSpan(details.JobRunTime)));
        sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Trigger Name").Replace("{{VALUE}}", HttpUtility.HtmlEncode(details.TriggerName)));
        sb.AppendLine("</table>");

        if (!string.IsNullOrEmpty(details.Exception))
        {
            sb.AppendLine("<p>");
            sb.AppendLine(HttpUtility.HtmlEncode(details.Exception));
            sb.AppendLine("</p>");
        }

        return template.Replace("{{TABLE}}", sb.ToString());
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
        if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
        return $"{timeSpan:hh\\:mm\\:ss}";
    }
}