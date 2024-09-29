using Common;
using Microsoft.Extensions.Configuration;

namespace RedisCheck;

internal class Defaults : BaseDefault, IRedisDefaults
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(1);
        Database = 0;
    }

    public int? Database { get; }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}