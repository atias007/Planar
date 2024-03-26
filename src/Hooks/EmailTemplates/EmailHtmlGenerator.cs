using Planar.Hook;
using System.Text;
using WebMarkupMin.Core;

namespace Planar.Hooks.EmailTemplates
{
    internal static class EmailHtmlGenerator
    {
        public static string GetEmailHtml(IMonitorDetails details)
        {
            var html = GetResource("AlertTemplate1");
            html = Replace(html, "Event Title", details.EventTitle);
            html = Replace(html, "Title", details.MonitorTitle);
            html = Replace(html, "Job Key", $"{details.JobGroup}.{details.JobName}");
            html = Replace(html, "Job Description", details.JobDescription);
            html = Replace(html, "Author", details.Author);
            html = Replace(html, "Fire Instance Id", details.FireInstanceId);
            html = Replace(html, "Fire Time", $"{details.FireTime}");
            html = Replace(html, "Run Time", FormatTimeSpan(details.JobRunTime));
            html = Replace(html, "Trigger Name", details.TriggerName);

            if (!string.IsNullOrWhiteSpace(details.Exception))
            {
                html = html.Replace("style=\"visibility:collapse;", "style=\"visibility:visible;");
                html = Replace(html, "Error Message", details.MostInnerExceptionMessage);
                html = Replace(html, "Exception Details", details.Exception);
            }

            html = MinifyHtml(html);
            return html;
        }

        public static string GetEmailHtml(IMonitorSystemDetails details)
        {
            var html = GetResource("AlertTemplate2");
            html = Replace(html, "Event Title", details.EventTitle);
            html = Replace(html, "Message", details.Message);

            var sb = new StringBuilder();
            var row = GetResource("AlertTemplateTableRow");
            foreach (var (key, value) in details.MessagesParameters)
            {
                var item = row.Replace("{{Key}}", key).Replace("{{Value}}", value);
                sb.Append(item);
            }

            html = html.Replace("<!-- {{Parameters}} -->", sb.ToString());
            html = MinifyHtml(html);
            return html;
        }

        private static string Replace(string html, string key, string? value)
        {
            return html.Replace($"{{{{{key}}}}}", value);
        }

        private static string GetResource(string templateName)
        {
            var resourceName = $"{nameof(Planar)}.{nameof(Hooks)}.EmailTemplates.{templateName}.html";
            return GetResourceByName(resourceName);
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
            if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
            return $"{timeSpan:hh\\:mm\\:ss}";
        }

        private static string GetResourceByName(string resourceName)
        {
            var assembly = typeof(EmailHtmlGenerator).Assembly ??
               throw new InvalidOperationException("Assembly is null");
            using var stream = assembly.GetManifestResourceStream(resourceName) ??
                throw new InvalidOperationException($"Resource '{resourceName}' not found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        private static string MinifyHtml(string html)
        {
            try
            {
                var htmlMinifier = new HtmlMinifier();
                var result = htmlMinifier.Minify(html, generateStatistics: false);
                if (result.Errors.Count == 0) { return result.MinifiedContent; }
                return html;
            }
            catch
            {
                return html;
            }
        }
    }
}