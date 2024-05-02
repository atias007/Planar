using Common;
using Microsoft.Extensions.Configuration;
using RedisCheck;

namespace RedisStreamCheck;

internal class Defaults : BaseDefault, IRedisDefaults
{
    public Defaults(IConfigurationSection section) : base(section)
    {
    }

    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(1);
        MaximumFailsInRow = 5;
    }

    public int? Database { get; set; } = 0;

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}