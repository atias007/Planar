using Planar.Hook;
using RestSharp;
using System.Text.RegularExpressions;

namespace Aig.Planar.SmsHook;

public partial class Hook : BaseHook
{
    public override string Name => "AigSmsHook";

    public override string Description => "AIG SMS hook";

    public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        base.LogInformation("1 - AigSmsHook is handling system event");
        return Task.CompletedTask;
    }

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        base.LogInformation("1 - AigSmsHook is handling job event");
        var messageJob = $"Planar Job: {monitorDetails.JobName}, Event: {monitorDetails.EventTitle}, Author: {monitorDetails.Author}";
        if (monitorDetails.Exception != null)
        {
            messageJob = $"{messageJob}, Error Message: {monitorDetails.Exception}";
        }

        var list = GetMessages(monitorDetails, messageJob);
        await SendMessage(list, monitorDetails);
    }

    private static List<string> GetPhoneNumbers(IEnumerable<IMonitorUser> users)
    {
        var allPhones = users.Select(u => u.PhoneNumber1 ?? string.Empty).Union(
          users.Select(u => u.PhoneNumber2 ?? string.Empty)).Union(
          users.Select(u => u.PhoneNumber3 ?? string.Empty)).ToList();
        return allPhones;
    }

    private static string GetInnerExceptionMessage(Exception ex)
    {
        if (ex.InnerException == null) { return ex.Message; }
        return GetInnerExceptionMessage(ex.InnerException);
    }

    private IEnumerable<SmsMessage> GetMessages(IMonitorDetails monitorDetails, string message)
    {
        LogInformation("2 - AigSmsHook GetMessages");
        var allPhones = GetPhoneNumbers(monitorDetails.Users);
        var sourceSystem = monitorDetails.GlobalConfig["SmsHook:SourceSystem"] ?? string.Empty;
        ArgumentException.ThrowIfNullOrEmpty(sourceSystem);
        var result = allPhones
            .Where(IsCellPhoneNumber)
            .Select(phone => new SmsMessage
            {
                MessageText = message,
                ToPhone = phone,
                SourceSystem = sourceSystem,
                OverrideWorkingHours = true
            });

        LogInformation($"3 - Message Count {result.Count()}");

        return result;
    }

    private async Task SendMessage(IEnumerable<SmsMessage> request, IMonitorDetails monitorDetails)
    {
        LogInformation("4 - AigSmsHook SendMessage");
        var url = monitorDetails.GlobalConfig["SmsHook:CommonServicesUrl"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("SmsHook:CommonServicesUrl is empty");
        }

        var options = new RestClientOptions
        {
            ThrowOnAnyError = true,
            BaseUrl = new Uri(url)
        };

        var client = new RestClient(options);
        var restRequest = new RestRequest("WebApi/api/Sms/SendMessage", HttpMethod.Post);
        var aggregateException = new List<Exception>();

        foreach (var item in request)
        {
            try
            {
                restRequest.AddBody(item);
                LogInformation($"5* - AigSmsHook ExecuteAsync. {item.ToPhone}");
                var result = await client.ExecuteAsync(restRequest);
                LogInformation($"6* - AigSmsHook ExecuteAsync. {result.StatusCode})");
            }
            catch (Exception ex)
            {
                aggregateException.Add(ex);
            }
        }

        if (aggregateException.Count != 0)
        {
            throw new AggregateException("Planar hooks has error", aggregateException);
        }
    }

    private static bool IsCellPhoneNumber(string? value)
    {
        if (string.IsNullOrEmpty(value)) { return false; }
        var regex = CellPhoneRegex();
        return regex.IsMatch(value);
    }

    [GeneratedRegex("^05\\d{8}$")]
    private static partial Regex CellPhoneRegex();
}

internal class SmsMessage
{
    public required string MessageText { get; set; }
    public required string ToPhone { get; set; }
    public required string SourceSystem { get; set; }
    public bool OverrideWorkingHours { get; set; }
}