using Planar.Hook;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Customs.SmsMonitorHook
{
    public partial class Hook : BaseHook
    {
        public override string Name => "CustomsSmsHook";

        public override string Description => "Send SMS message to all users in monitor group, using Customs PELEPHONE account";

        public override Task Handle(IMonitorDetails monitorDetails)
        {
            var message = GetMessageText(monitorDetails);
            Parallel.ForEach(monitorDetails.Users, user =>
            {
                Task.WaitAll(
                    SendSms(user.PhoneNumber1, message),
                    SendSms(user.PhoneNumber2, message),
                    SendSms(user.PhoneNumber3, message)
                );
            });

            return Task.CompletedTask;
        }

        public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
        {
            var message = GetMessageText(monitorDetails);
            Parallel.ForEach(monitorDetails.Users, user =>
            {
                Task.WaitAll(
                    SendSms(user.PhoneNumber1, message),
                    SendSms(user.PhoneNumber2, message),
                    SendSms(user.PhoneNumber3, message)
                );
            });

            return Task.CompletedTask;
        }

        private static bool IsCellphoneValid(string value)
        {
            if (string.IsNullOrEmpty(value)) { return false; }
            return CellPhoneRegex().IsMatch(value);
        }

        private static string? SafeTrim(string? value)
        {
            if (string.IsNullOrEmpty(value)) { return value; }
            return value.Trim();
        }

        private async Task SendSms(string? to, string message)
        {
            to = SafeTrim(to);
            if (string.IsNullOrEmpty(to)) return;
            var valid = IsCellphoneValid(to);

            if (!valid)
            {
                return;
            }

            const string xmlPattern = "<PALO><HEAD><FROM>customs</FROM><APP USER=\"customs\" PASSWORD=\"2UcOonPH\">LA</APP><CMD>sendtextmt</CMD></HEAD><BODY><SENDER>PLANAR</SENDER><CONTENT>{1}</CONTENT><DEST_LIST><TO>{0}</TO></DEST_LIST></BODY><OPTIONAL><CATEGORY>APP</CATEGORY></OPTIONAL></PALO>";

            var xml = string.Format(xmlPattern, to, message);
            var encodedXml = HttpUtility.UrlEncode(xml);

            var responseText = string.Empty;
            var request = WebRequest.Create("https://la2.pelephone.co.il/unistart5.asp");
            try
            {
                request.Timeout = 20000;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                var postBuffer = Encoding.UTF8.GetBytes("XMLString=" + encodedXml);
                request.ContentLength = postBuffer.Length;
                using var requestStream = request.GetRequestStream();
                requestStream.Write(postBuffer, 0, postBuffer.Length);

                using var response = await request.GetResponseAsync();
                using var srteamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseText = srteamReader.ReadToEnd();
                LogInformation(responseText);
            }
            catch (Exception ex)
            {
                LogError($"Fail to send sms to {to}. Error message: {ex.Message}");
            }

            try
            {
                // analayze the response
                var doc = XDocument.Parse(responseText);
                var root = doc.Root;
                if (root != null && string.Equals(root.Name.LocalName, "palo", StringComparison.OrdinalIgnoreCase))
                {
                    var element = root.Element("RESULT")?.Value;
                    if (string.Equals(element, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return; // Success
                    }

                    var error = "Unhadled error from SMS supllier";
                    if (string.Equals(element, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        var description = root.Element("DESCRIPTION")?.Value;
                        if (!string.IsNullOrEmpty(description))
                        {
                            var hostName = Dns.GetHostName(); // Retrive the Name of HOST
                            var myIP = Dns.GetHostEntry(hostName)?.AddressList.FirstOrDefault()?.ToString(); // Get the IP
                            error = description;

                            if (string.Equals(error, "Invalid account", StringComparison.OrdinalIgnoreCase))
                            {
                                error = $"{error} - Check whether the external IP address provided By THILA on the scheduler Server for the user scheduler-svc is defined for us by Pelephone. IP: {myIP}";
                            }
                        }
                    }

                    LogError($"Fail to send sms to {to}. Supplier error code: {error}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Fail to analyze the response of sms supplier\r\n{responseText}\r\nError message: {ex.Message}");
            }
        }

        private static string GetMessageText(IMonitorDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Planar job '{details.JobGroup}.{details.JobName}' alert event {details.EventTitle}");
            sb.AppendLine($"Fire Time: {details.FireTime:g}");
            sb.AppendLine($"Job Run Time: {details.JobRunTime:hh\\:mm\\:ss}");
            sb.AppendLine($"Fire Instance Id: {details.FireInstanceId}");
            if (!string.IsNullOrWhiteSpace(details.MostInnerExceptionMessage))
            {
                sb.AppendLine($"\r\nException: {details.MostInnerExceptionMessage}");
            }

            return sb.ToString();
        }

        private static string GetMessageText(IMonitorSystemDetails details)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Planar job system alert event {details.EventTitle}");
            sb.AppendLine($"Monitor Title: {details.MonitorTitle}");
            sb.AppendLine($"Message: {details.Message}");
            if (details.Exception != null)
            {
                sb.AppendLine($"\r\nException: {details.MostInnerExceptionMessage}");
            }

            return sb.ToString();
        }

        private Exception GetMostInnerException(Exception ex)
        {
            if (ex.InnerException == null) { return ex; }
            return GetMostInnerException(ex.InnerException);
        }

        [GeneratedRegex("^0\\d{9}$")]
        private static partial Regex CellPhoneRegex();
    }
}