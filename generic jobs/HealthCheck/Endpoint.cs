using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint : BaseDefault, IEndpoint, ICheckElemnt
{
    public Endpoint(IConfigurationSection section, string url)
    {
        Url = url;

        Name = section.GetValue<string?>("name");
        SuccessStatusCodes = section.GetSection("success status codes").Get<int[]?>();
        Timeout = section.GetValue<TimeSpan?>("timeout");

        //// --------------------------------------- ////

        RetryCount = section.GetValue<int?>("retry count");
        RetryInterval = section.GetValue<TimeSpan?>("retry interval");
        MaximumFailsInRow = section.GetValue<int?>("maximum fails in row");
    }

    public string? Name { get; set; }
    public string Url { get; private set; }
    public IEnumerable<int>? SuccessStatusCodes { get; set; }
    public TimeSpan? Timeout { get; set; }

    public string Key => Url;
}