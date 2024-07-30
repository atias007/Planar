using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Defaults : BaseDefault, IEndpoint
{
    public Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(10);
        MaximumFailsInRow = 5;
        SuccessStatusCodes = new List<int> { 200, 201, 202, 204 };
        Timeout = TimeSpan.FromSeconds(5);
    }

    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
        SuccessStatusCodes = new List<int> { 200, 201, 202, 204 };
        Timeout = TimeSpan.FromSeconds(5);
    }

    public IEnumerable<int> SuccessStatusCodes { get; }
    public TimeSpan Timeout { get; }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}