using MailKit.Net.Smtp;
using MimeKit;
using Planar.Monitor.Hook;
using System.Text;

namespace Planar.Hooks
{
    public class PlanarSmtpHook : BaseHook
    {
        public override string Name => "Planar.Smtp";

        public override async Task Handle(IMonitorDetails monitorDetails)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Planar", "admin@planar.me"));
            var emails1 = monitorDetails.Users.Select(u => u.EmailAddress1);
            var emails2 = monitorDetails.Users.Select(u => u.EmailAddress2);
            var emails3 = monitorDetails.Users.Select(u => u.EmailAddress3);
            var allEmails = emails1.Concat(emails2).Concat(emails3).Distinct();

            foreach (var recipient in allEmails)
            {
                if (!string.IsNullOrEmpty(recipient))
                {
                    message.To.Add(new MailboxAddress(recipient, recipient));
                }
            }

            message.Subject = $"Planar Monitor Alert. Name: \"{monitorDetails.MonitorTitle}\", Event: \"{monitorDetails.EventTitle}\"";
            var body = new BodyBuilder
            {
                TextBody = GetText(monitorDetails),
                HtmlBody = GetHtml(monitorDetails),
            }.ToMessageBody();

            message.Body = body;

            using var client = new SmtpClient();
            var tokenSource = new CancellationTokenSource(30000);
            client.Connect("smtp.gmail.com", port: 587, useSsl: false, tokenSource.Token);
            client.Authenticate("tsahiatias@gmail.com", "abbconwfeacvwklo", tokenSource.Token);
            await client.SendAsync(message, tokenSource.Token);
            client.Disconnect(quit: true, tokenSource.Token);
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            throw new NotImplementedException();
        }

        private static string GetText(IMonitorDetails details)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Title: {details.MonitorTitle}");
            sb.AppendLine($"Event Title: {details.EventTitle}");
            sb.AppendLine($"Event: {details.EventTitle}");
            sb.AppendLine($"Job: {details.JobGroup}.{details.JobName}");
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
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Title").Replace("{{VALUE}}", details.MonitorTitle));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Event").Replace("{{VALUE}}", details.EventTitle));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Job Key").Replace("{{VALUE}}", $"{details.JobGroup}.{details.JobName}"));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Job Description").Replace("{{VALUE}}", details.JobDescription));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Author").Replace("{{VALUE}}", details.Author));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Fire Instance Id").Replace("{{VALUE}}", details.FireInstanceId));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Fire Time").Replace("{{VALUE}}", details.FireTime.ToString()));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Run Time").Replace("{{VALUE}}", FormatTimeSpan(details.JobRunTime)));
            sb.AppendLine(itempTemplate.Replace("{{KEY}}", "Trigger Name").Replace("{{VALUE}}", details.TriggerName));
            sb.AppendLine("</table>");
            return template.Replace("{{TABLE}}", sb.ToString());
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
            if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
            return $"{timeSpan:hh\\:mm\\:ss}";
        }
    }
}