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
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}