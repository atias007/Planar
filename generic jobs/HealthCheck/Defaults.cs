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
    }

    public Defaults(IConfigurationSection section) : base(section)
    {
    }

    public IEnumerable<int>? SuccessStatusCodes { get; set; } = new List<int> { 200, 201, 202, 204 };
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}